using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async void WishingTokenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isWishingTokenRunning)
            {
                RequestWishingTokenStop();
                return;
            }

            if (!_botChatClient.IsReady)
            {
                _log.Info("WebView2 not ready");
                return;
            }

            await RunWishingTokenLoopAsync();
        }

        private async Task RunWishingTokenLoopAsync()
        {
            var shouldResumeEngine = _engine != null && _engine.IsRunning;
            SetWishingTokenRunning(true);
            _wishingTokenCancellation = new CancellationTokenSource();
            _log.Info("[wishing token] Started.");

            try
            {
                if (shouldResumeEngine)
                {
                    _log.Info("[wishing token] Pausing bot automation.");
                    await _engine.StopAsync();
                    _log.Engine("Engine paused for wishing token");
                }

                await _wishingTokenWorkflow.RunAsync(LogWishingTokenInfo, _wishingTokenCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                _log.Info("[wishing token] Stopped by user.");
            }
            catch (Exception ex)
            {
                _log.Warning("[wishing token] failed: " + ex.Message);
            }
            finally
            {
                _wishingTokenCancellation.Dispose();
                _wishingTokenCancellation = null;
                SetWishingTokenRunning(false);

                if (shouldResumeEngine)
                {
                    _log.Info("[wishing token] Resuming bot automation.");
                    await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed after wishing token");
                }
            }
        }

        private void RequestWishingTokenStop()
        {
            if (_wishingTokenCancellation == null || _wishingTokenCancellation.IsCancellationRequested)
            {
                return;
            }

            _log.Info("[wishing token] Stop requested.");
            _wishingTokenCancellation.Cancel();
        }

        private void SetWishingTokenRunning(bool isRunning)
        {
            _isWishingTokenRunning = isRunning;
            WishingTokenBtn.Content = isRunning ? "Stop wishing token" : "Wishing token";
        }

        private bool ShouldBlockForWishingToken(string actionName)
        {
            if (!_isWishingTokenRunning)
            {
                return false;
            }

            _log.Info($"[wishing token] {actionName} ignored while the wishing-token loop is running.");
            return true;
        }

        private void LogWishingTokenInfo(string message)
        {
            UiDispatcher.OnUI(() => _log.Info("[wishing token] " + message));
        }
    }
}
