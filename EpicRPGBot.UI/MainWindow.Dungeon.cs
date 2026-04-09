using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async void CompleteDungeonBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isDungeonRunning)
            {
                _dungeonCancellation?.Cancel();
                _log.Info("[dungeon] Cancellation requested.");
                return;
            }

            if (ShouldBlockForExclusiveBotOperation("Complete Dungeon"))
            {
                return;
            }

            if (!_dungeonChatClient.IsReady)
            {
                _log.Info("Dungeon tab not ready");
                return;
            }

            if (!_botChatClient.IsReady)
            {
                _log.Info("Bot tab not ready");
                return;
            }

            if (!TryBeginExclusiveBotOperation("Complete Dungeon"))
            {
                return;
            }

            _dungeonCancellation?.Dispose();
            _dungeonCancellation = new CancellationTokenSource();
            SetDungeonRunning(true);
            SelectDungeonTab();
            _log.Info("[dungeon] Started.");

            try
            {
                var result = await RunDungeonJobAsync(_log.Info, _dungeonCancellation.Token);
                _log.Info("[dungeon] " + result.Summary);
            }
            catch (OperationCanceledException)
            {
                _log.Info("[dungeon] Cancelled.");
            }
            catch (Exception ex)
            {
                _log.Warning("[dungeon] failed: " + ex.Message);
            }
            finally
            {
                _dungeonCancellation.Dispose();
                _dungeonCancellation = null;
                SetDungeonRunning(false);
                EndExclusiveBotOperation("Complete Dungeon");
            }
        }

        private async Task<Dungeon.DungeonRunResult> RunDungeonJobAsync(Action<string> report, CancellationToken cancellationToken)
        {
            var shouldResumeEngine = _engine != null && _engine.IsRunning;
            if (shouldResumeEngine)
            {
                report?.Invoke("Pausing bot automation.");
                await _engine.StopAsync();
                _log.Engine("Engine paused for dungeon automation");
            }

            try
            {
                return await _completeDungeonRunCoordinator.RunAsync(
                    RunPreDungeonAreaTradeAsync,
                    RunDungeonListingPhaseAsync,
                    report,
                    cancellationToken);
            }
            finally
            {
                if (shouldResumeEngine)
                {
                    report?.Invoke("[dungeon] Resuming bot automation.");
                    await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed after dungeon automation");
                }
            }
        }

        private async Task<Crafting.CraftJobResult> RunPreDungeonAreaTradeAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            SelectBotTab();
            report?.Invoke("[dungeon] Navigating the bot tab to the default channel URL for pre-dungeon trading.");
            await _botChatClient.NavigateToChannelAsync(GetCurrentSettings().ResolveChannelUrl());
            return await RunAreaTradeJobAsync(report, cancellationToken);
        }

        private async Task<Dungeon.DungeonRunResult> RunDungeonListingPhaseAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            SelectDungeonTab();
            return await _dungeonWorkflow.RunAsync(message => report?.Invoke("[dungeon] " + message), cancellationToken);
        }

        private void SetDungeonRunning(bool isRunning)
        {
            _isDungeonRunning = isRunning;
            RefreshBotControlButtonColors();
        }
    }
}
