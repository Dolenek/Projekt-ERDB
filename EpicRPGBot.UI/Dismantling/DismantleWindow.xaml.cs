using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI.Dismantling
{
    public partial class DismantleWindow : Window
    {
        private readonly Func<DismantleRequest, Action<string>, CancellationToken, Task<CraftJobResult>> _runDismantleAsync;
        private CancellationTokenSource _dismantlingCancellation;
        private bool _closeWhenIdle;
        private bool _isDismantling;

        public DismantleWindow(Func<DismantleRequest, Action<string>, CancellationToken, Task<CraftJobResult>> runDismantleAsync)
        {
            _runDismantleAsync = runDismantleAsync ?? throw new ArgumentNullException(nameof(runDismantleAsync));
            InitializeComponent();
            ApplyAutomationSurface();
            SetDefaultAmounts();
        }

        private async void DismantleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isDismantling)
            {
                return;
            }

            if (!TryBuildRequest(out var request))
            {
                return;
            }

            _dismantlingCancellation = new CancellationTokenSource();
            SetDismantlingState(true);
            AppendStatusLine("Dismantling started.");

            try
            {
                var result = await _runDismantleAsync(request, AppendStatusLine, _dismantlingCancellation.Token);
                AppendStatusLine(result.Summary);
            }
            catch (Exception ex)
            {
                AppendStatusLine("Dismantling failed: " + ex.Message);
            }
            finally
            {
                _dismantlingCancellation.Dispose();
                _dismantlingCancellation = null;
                SetDismantlingState(false);
                if (_closeWhenIdle)
                {
                    Close();
                }
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isDismantling)
            {
                Close();
                return;
            }

            if (_closeWhenIdle)
            {
                return;
            }

            _closeWhenIdle = true;
            AppendStatusLine("Cancellation requested. Waiting for the current command to finish.");
            _dismantlingCancellation?.Cancel();
            CancelBtn.Content = "Cancelling...";
        }

        private bool TryBuildRequest(out DismantleRequest request)
        {
            request = null;

            var selections = new[]
            {
                CreateSelection(CraftItemKey.UltraLog, "ultra log", UltraAmountBox),
                CreateSelection(CraftItemKey.HyperLog, "hyper log", HyperAmountBox),
                CreateSelection(CraftItemKey.MegaLog, "mega log", MegaAmountBox),
                CreateSelection(CraftItemKey.SuperLog, "super log", SuperAmountBox),
                CreateSelection(CraftItemKey.EpicLog, "epic log", EpicAmountBox),
                CreateSelection(CraftItemKey.EpicFish, "epic fish", EpicFishAmountBox),
                CreateSelection(CraftItemKey.GoldenFish, "golden fish", GoldenFishAmountBox),
                CreateSelection(CraftItemKey.Banana, "banana", BananaAmountBox)
            };

            var activeSelections = selections.Where(selection => !selection.IsEmpty).ToArray();
            if (activeSelections.Length == 0)
            {
                AppendStatusLine("Enter an amount or 'all' in one row.");
                return false;
            }

            if (activeSelections.Length > 1)
            {
                AppendStatusLine("Enter an amount in exactly one row.");
                return false;
            }

            var selected = activeSelections[0];
            if (selected.IsAll)
            {
                request = new DismantleRequest(selected.ItemKey, true, 0);
                return true;
            }

            if (selected.Amount <= 0)
            {
                AppendStatusLine($"Invalid amount for {selected.ItemName}: {selected.RawValue}");
                return false;
            }

            request = new DismantleRequest(selected.ItemKey, false, selected.Amount);
            return true;
        }

        private Selection CreateSelection(CraftItemKey itemKey, string itemName, TextBox textBox)
        {
            var rawValue = textBox.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(rawValue) || rawValue == "0")
            {
                return new Selection(itemKey, itemName, rawValue, true, false, 0);
            }

            if (string.Equals(rawValue, "all", StringComparison.OrdinalIgnoreCase))
            {
                return new Selection(itemKey, itemName, rawValue, false, true, 0);
            }

            return long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var amount)
                ? new Selection(itemKey, itemName, rawValue, false, false, amount)
                : new Selection(itemKey, itemName, rawValue, false, false, -1);
        }

        private void AppendStatusLine(string message)
        {
            Dispatcher.Invoke(() =>
            {
                var line = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
                if (string.IsNullOrEmpty(line))
                {
                    return;
                }

                StatusBox.AppendText(line + Environment.NewLine);
                StatusBox.ScrollToEnd();
            });
        }

        private void SetDismantlingState(bool isDismantling)
        {
            _isDismantling = isDismantling;
            DismantleBtn.IsEnabled = !isDismantling;
            UltraAmountBox.IsEnabled = !isDismantling;
            HyperAmountBox.IsEnabled = !isDismantling;
            MegaAmountBox.IsEnabled = !isDismantling;
            SuperAmountBox.IsEnabled = !isDismantling;
            EpicAmountBox.IsEnabled = !isDismantling;
            EpicFishAmountBox.IsEnabled = !isDismantling;
            GoldenFishAmountBox.IsEnabled = !isDismantling;
            BananaAmountBox.IsEnabled = !isDismantling;
            CancelBtn.Content = isDismantling ? "Cancel" : "Close";
        }

        private void SetDefaultAmounts()
        {
            UltraAmountBox.Text = "0";
            HyperAmountBox.Text = "0";
            MegaAmountBox.Text = "0";
            SuperAmountBox.Text = "0";
            EpicAmountBox.Text = "0";
            EpicFishAmountBox.Text = "0";
            GoldenFishAmountBox.Text = "0";
            BananaAmountBox.Text = "0";
        }

        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "DismantleWindow");
            SetAutomationIdentity(UltraAmountBox, "DismantleUltraAmountInput");
            SetAutomationIdentity(HyperAmountBox, "DismantleHyperAmountInput");
            SetAutomationIdentity(MegaAmountBox, "DismantleMegaAmountInput");
            SetAutomationIdentity(SuperAmountBox, "DismantleSuperAmountInput");
            SetAutomationIdentity(EpicAmountBox, "DismantleEpicAmountInput");
            SetAutomationIdentity(EpicFishAmountBox, "DismantleEpicFishAmountInput");
            SetAutomationIdentity(GoldenFishAmountBox, "DismantleGoldenFishAmountInput");
            SetAutomationIdentity(BananaAmountBox, "DismantleBananaAmountInput");
            SetAutomationIdentity(StatusBox, "DismantleStatusText");
            SetAutomationIdentity(DismantleBtn, "DismantleStartButton");
            SetAutomationIdentity(CancelBtn, "DismantleCancelButton");
        }

        private static void SetAutomationIdentity(DependencyObject element, string automationId)
        {
            if (element == null || string.IsNullOrWhiteSpace(automationId))
            {
                return;
            }

            AutomationProperties.SetAutomationId(element, automationId);
        }

        private sealed class Selection
        {
            public Selection(CraftItemKey itemKey, string itemName, string rawValue, bool isEmpty, bool isAll, long amount)
            {
                ItemKey = itemKey;
                ItemName = itemName;
                RawValue = rawValue;
                IsEmpty = isEmpty;
                IsAll = isAll;
                Amount = amount;
            }

            public CraftItemKey ItemKey { get; }
            public string ItemName { get; }
            public string RawValue { get; }
            public bool IsEmpty { get; }
            public bool IsAll { get; }
            public long Amount { get; }
        }
    }
}
