using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async void TradeAreaBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShouldBlockForWishingToken("Trade area"))
            {
                return;
            }

            if (_isAreaTradeRunning)
            {
                _log.Info("Area trade already running.");
                return;
            }

            if (!_botChatClient.IsReady)
            {
                _log.Info("WebView2 not ready");
                return;
            }

            SetAreaTradeRunning(true);
            _log.Info("Area trade started.");

            try
            {
                var result = await RunAreaTradeJobAsync(_log.Info, CancellationToken.None);
                _log.Info("[trade area] " + result.Summary);
            }
            catch (Exception ex)
            {
                _log.Warning("[trade area] failed: " + ex.Message);
            }
            finally
            {
                SetAreaTradeRunning(false);
            }
        }

        private async Task<CraftJobResult> RunAreaTradeJobAsync(Action<string> report, CancellationToken cancellationToken)
        {
            var shouldResumeEngine = _engine != null && _engine.IsRunning;
            if (shouldResumeEngine)
            {
                report?.Invoke("Pausing bot automation.");
                await _engine.StopAsync();
                _log.Engine("Engine paused for area trading");
            }

            try
            {
                return await _areaTradeWorkflow.RunAsync(message =>
                {
                    report?.Invoke("[trade area] " + message);
                }, cancellationToken);
            }
            finally
            {
                if (shouldResumeEngine)
                {
                    report?.Invoke("[trade area] Resuming bot automation.");
                    await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed after area trading");
                }
            }
        }

        private void SetAreaTradeRunning(bool isRunning)
        {
            _isAreaTradeRunning = isRunning;
            TradeAreaBtn.IsEnabled = !isRunning;
        }
    }
}
