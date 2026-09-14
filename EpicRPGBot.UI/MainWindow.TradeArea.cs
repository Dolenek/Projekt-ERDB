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
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
            var browserLease = await AcquireBotWorkflowAsync();
            var operationStarted = false;
            try
            {
            if (ShouldBlockForExclusiveBotOperation("Trade area"))
            {
                return;
            }

            if (!TryBeginExclusiveBotOperation("Trade area")) return;
            operationStarted = true;

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
            finally
            {
                if (operationStarted) EndExclusiveBotOperation("Trade area");
                await ReleaseStoppedEngineDemandAsync();
                await browserLease.ReleaseAsync();
            }
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
            CurrentAccount.NotifyStateChanged();
            if (ReferenceEquals(CurrentAccount, _activeAccountRuntime))
                TradeAreaBtn.IsEnabled = !isRunning;
        }
    }
}
