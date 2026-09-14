using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const string DuelOperationName = "Duel";
        private async void DuelBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
            if (_isDuelRunning)
            {
                RequestDuelCancellation();
                return;
            }

            if (ShouldBlockForExclusiveBotOperation(DuelOperationName))
            {
                return;
            }

            if (!TryBeginExclusiveBotOperation(DuelOperationName))
            {
                return;
            }

            BeginDuelRun();
            await RunDuelWithWebViewAsync();
            }
        }

        private void RequestDuelCancellation()
        {
            _duelCancellation?.Cancel();
            _log.Info("[duel] Cancellation requested.");
        }

        private async Task RunDuelWithWebViewAsync()
        {
            Services.DiscordWebViewLease webViewLease = null;
            Services.DiscordWebViewLease botWebViewLease = null;
            try
            {
                botWebViewLease = await AcquireBotWorkflowAsync();
                webViewLease = await _duelWebViewSession.AcquireAsync(
                    Services.DiscordWebViewActivityReason.Workflow,
                    _duelCancellation.Token);
                await ExecuteDuelWorkflowAsync();
            }
            catch (OperationCanceledException)
            {
                _log.Info("[duel] Cancelled.");
            }
            catch (Exception ex)
            {
                _log.Warning("[duel] failed: " + ex.Message);
            }
            finally
            {
                EndDuelRun();
                if (webViewLease != null)
                {
                    await webViewLease.ReleaseAsync();
                }
                if (botWebViewLease != null)
                {
                    await ReleaseStoppedEngineDemandAsync();
                    await botWebViewLease.ReleaseAsync();
                }
            }
        }

        private async Task ExecuteDuelWorkflowAsync()
        {
            var result = await _duelWorkflow.RunAsync(
                message => _log.Info("[duel] " + message),
                PauseBotForDuelAsync,
                ResumeBotForDuelMatchmakingAsync,
                _duelCancellation.Token);
            _log.Info("[duel] " + result.Summary);
            if (!result.RequiresBotToRemainStopped)
            {
                await ResumeBotForDuelMatchmakingAsync();
            }
        }

        private void BeginDuelRun()
        {
            _duelInitialEngineWasRunning = _engine != null && _engine.IsRunning;
            _duelCancellation?.Dispose();
            _duelCancellation = new CancellationTokenSource();
            _isDuelRunning = true;
            RefreshBotControlButtonColors();
            _log.Info("[duel] Started.");
        }

        private void EndDuelRun()
        {
            _duelCancellation?.Dispose();
            _duelCancellation = null;
            _isDuelRunning = false;
            RefreshBotControlButtonColors();
            EndExclusiveBotOperation(DuelOperationName);
        }

        private async Task PauseBotForDuelAsync()
        {
            if (_engine == null || !_engine.IsRunning)
            {
                return;
            }

            await _engine.StopAsync();
            _log.Engine("Engine paused for duel automation");
        }

        private async Task ResumeBotForDuelMatchmakingAsync()
        {
            if (!_duelInitialEngineWasRunning || (_engine != null && _engine.IsRunning))
            {
                return;
            }

            await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed during duel matchmaking");
        }
    }
}
