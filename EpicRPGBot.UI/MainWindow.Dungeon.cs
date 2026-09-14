using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const string DungeonOperationName = "Complete Dungeon";

        private async void CompleteDungeonBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
            if (_isDungeonRunning)
            {
                RequestDungeonCancellation();
                return;
            }

            if (ShouldBlockForExclusiveBotOperation(DungeonOperationName))
            {
                return;
            }

            if (!TryBeginExclusiveBotOperation(DungeonOperationName))
            {
                return;
            }

            BeginDungeonRun();
            await RunDungeonWithWebViewAsync();
            }
        }

        private void RequestDungeonCancellation()
        {
            _dungeonCancellation?.Cancel();
            _log.Info("[dungeon] Cancellation requested.");
        }

        private void BeginDungeonRun()
        {
            _dungeonCancellation?.Dispose();
            _dungeonCancellation = new CancellationTokenSource();
            SetDungeonRunning(true);
            SelectDungeonTab();
            _log.Info("[dungeon] Started.");
        }

        private async Task RunDungeonWithWebViewAsync()
        {
            Services.DiscordWebViewLease webViewLease = null;
            Services.DiscordWebViewLease botWebViewLease = null;
            try
            {
                botWebViewLease = await AcquireBotWorkflowAsync();
                webViewLease = await _dungeonWebViewSession.AcquireAsync(
                    Services.DiscordWebViewActivityReason.Workflow,
                    _dungeonCancellation.Token);
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
                EndExclusiveBotOperation(DungeonOperationName);
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
                    async (phaseReport, phaseCancellationToken) =>
                    {
                        await ResumeBotAutomationForDungeonMatchmakingAsync(phaseReport);
                        return await RunDungeonListingPhaseAsync(phaseReport, phaseCancellationToken);
                    },
                    report,
                    cancellationToken);
            }
            finally
            {
                if (shouldResumeEngine && (_engine == null || !_engine.IsRunning))
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
            report?.Invoke("[dungeon] Using the current bot tab channel for pre-dungeon trading.");
            return await RunAreaTradeJobAsync(report, cancellationToken);
        }

        private async Task<Dungeon.DungeonRunResult> RunDungeonListingPhaseAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            SelectDungeonTab();
            return await _dungeonWorkflow.RunAsync(
                message => report?.Invoke("[dungeon] " + message),
                PauseBotAutomationAfterDungeonMatchAsync,
                cancellationToken);
        }

        private async Task ResumeBotAutomationForDungeonMatchmakingAsync(Action<string> report)
        {
            if (_engine == null || _engine.IsRunning)
            {
                return;
            }

            report?.Invoke("[dungeon] Resuming bot automation while waiting for a partner.");
            await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed during dungeon matchmaking");
        }

        private async Task PauseBotAutomationAfterDungeonMatchAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (_engine == null || !_engine.IsRunning)
            {
                return;
            }

            report?.Invoke("[dungeon] Partner found. Pausing bot automation before entering the dungeon.");
            await _engine.StopAsync();
            _log.Engine("Engine paused after dungeon partner was found");
        }

        private void SetDungeonRunning(bool isRunning)
        {
            _isDungeonRunning = isRunning;
            RefreshBotControlButtonColors();
        }
    }
}
