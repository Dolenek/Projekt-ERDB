using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const string DuelOperationName = "Duel";
        private bool _duelInitialEngineWasRunning;

        private async void DuelBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isDuelRunning)
            {
                _duelCancellation?.Cancel();
                _log.Info("[duel] Cancellation requested.");
                return;
            }

            if (ShouldBlockForExclusiveBotOperation(DuelOperationName) || !_duelChatClient.IsReady)
            {
                _log.Info("[duel] Duel tab is not ready or another workflow is active.");
                return;
            }

            if (!TryBeginExclusiveBotOperation(DuelOperationName))
            {
                return;
            }

            BeginDuelRun();
            try
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
            catch (Exception ex)
            {
                _log.Warning("[duel] failed: " + ex.Message);
            }
            finally
            {
                EndDuelRun();
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
