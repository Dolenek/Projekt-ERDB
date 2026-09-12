using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace EpicRPGBot.UI.Settings
{
    public partial class WorkCommandsWindow
    {
        private async void AutoBestBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_loadAutoBestWorkCommands == null || _autoBestCancellation != null)
            {
                return;
            }

            var cancellation = new CancellationTokenSource();
            _autoBestCancellation = cancellation;
            SetAutoBestLoading(true, "Loading Auto-Best profile data…");
            try
            {
                var result = await _loadAutoBestWorkCommands(cancellation.Token);
                if (cancellation.IsCancellationRequested)
                {
                    return;
                }

                if (result?.Success == true && !TryApplyAutoBestSelections(result.Selections))
                {
                    AutoBestStatusText.Text = "Auto-Best returned an incomplete area map.";
                    return;
                }

                AutoBestStatusText.Text = result?.Message ?? "Auto-Best did not return a result.";
            }
            catch (OperationCanceledException)
            {
                AutoBestStatusText.Text = "Auto-Best was cancelled.";
            }
            catch (Exception ex)
            {
                AutoBestStatusText.Text = "Auto-Best failed: " + ex.Message;
            }
            finally
            {
                CompleteAutoBestOperation(cancellation);
            }
        }

        private bool TryApplyAutoBestSelections(IReadOnlyDictionary<int, string> selections)
        {
            if (!ContainsEveryArea(selections))
            {
                return false;
            }

            _applyingBulkSelection = true;
            try
            {
                foreach (var row in Rows)
                {
                    row.CommandText = selections[row.Area];
                }
            }
            finally
            {
                _applyingBulkSelection = false;
            }

            PersistSelections();
            return true;
        }

        private bool ContainsEveryArea(IReadOnlyDictionary<int, string> selections)
        {
            if (selections == null || selections.Count != Rows.Count)
            {
                return false;
            }

            foreach (var row in Rows)
            {
                if (!selections.TryGetValue(row.Area, out var commandText) ||
                    string.IsNullOrWhiteSpace(commandText))
                {
                    return false;
                }
            }

            return true;
        }

        private void CompleteAutoBestOperation(CancellationTokenSource cancellation)
        {
            if (!ReferenceEquals(_autoBestCancellation, cancellation))
            {
                return;
            }

            _autoBestCancellation = null;
            cancellation.Dispose();
            SetAutoBestLoading(false, AutoBestStatusText.Text);
        }

        private void SetAutoBestLoading(bool isLoading, string status)
        {
            AutoBestBtn.IsEnabled = !isLoading && _loadAutoBestWorkCommands != null;
            AutoBestProgress.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            AutoBestStatusText.Text = status ?? string.Empty;
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            _autoBestCancellation?.Cancel();
        }
    }
}
