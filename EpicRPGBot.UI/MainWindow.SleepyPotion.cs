using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const string SleepyPotionOperationName = "sleepy potion";

        private async void SleepyPotionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isSleepyPotionRunning)
            {
                RequestSleepyPotionStop();
                return;
            }

            if (!TryBeginExclusiveBotOperation(SleepyPotionOperationName))
            {
                return;
            }

            if (!_botChatClient.IsReady)
            {
                _log.Info("WebView2 not ready");
                EndExclusiveBotOperation(SleepyPotionOperationName);
                return;
            }

            await RunSleepyPotionAsync();
        }

        private async Task RunSleepyPotionAsync()
        {
            var engineWasRunning = _engine != null && _engine.IsRunning;
            SetSleepyPotionRunning(true);
            _sleepyPotionCancellation = new CancellationTokenSource();
            _log.Info("[sleepy potion] Started.");

            try
            {
                var hasFreshSnapshot = false;
                if (!engineWasRunning)
                {
                    await StartEngineAsync("Engine started for sleepy potion");
                    var startupSnapshot = await SendExclusiveEngineCommandAsync("rpg cd", armStartupCutoff: true);
                    if (!startupSnapshot.IsConfirmed)
                    {
                        _log.Info("[sleepy potion] Stopped: failed to refresh cooldowns after starting the engine.");
                        return;
                    }

                    hasFreshSnapshot = true;
                }

                await RunSleepyPotionCoreAsync(hasFreshSnapshot, _sleepyPotionCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                _log.Info("[sleepy potion] Stopped by user.");
            }
            catch (Exception ex)
            {
                _log.Warning("[sleepy potion] failed: " + ex.Message);
            }
            finally
            {
                _sleepyPotionCancellation.Dispose();
                _sleepyPotionCancellation = null;
                SetSleepyPotionRunning(false);
                EndExclusiveBotOperation(SleepyPotionOperationName);

                if (!engineWasRunning && _engine != null && _engine.IsRunning)
                {
                    await _engine.StopAsync();
                    _log.Engine("Engine stopped after sleepy potion");
                }
            }
        }

        private async Task RunSleepyPotionCoreAsync(bool hasFreshSnapshot, CancellationToken cancellationToken)
        {
            if (!hasFreshSnapshot)
            {
                _log.Info("[sleepy potion] Refreshing cooldowns with 'rpg cd'.");
                var initialCdResult = await SendExclusiveEngineCommandAsync("rpg cd");
                if (!initialCdResult.IsConfirmed)
                {
                    _log.Info("[sleepy potion] Stopped: 'rpg cd' did not confirm.");
                    return;
                }
            }

            if (!await WaitForAutomatedCommandsToSettleAsync(cancellationToken))
            {
                _log.Info("[sleepy potion] Stopped: automated cooldown batch did not settle before the potion.");
                return;
            }

            _log.Info("[sleepy potion] Sending 'rpg egg use sleepy potion'.");
            var sleepyPotionResult = await SendExclusiveEngineCommandAsync("rpg egg use sleepy potion");
            if (!sleepyPotionResult.IsConfirmed)
            {
                _log.Info("[sleepy potion] Stopped: 'rpg egg use sleepy potion' did not confirm.");
                return;
            }

            _log.Info("[sleepy potion] Refreshing cooldowns with 'rpg cd'.");
            var followupCdResult = await SendExclusiveEngineCommandAsync("rpg cd");
            if (!followupCdResult.IsConfirmed)
            {
                _log.Info("[sleepy potion] Stopped: follow-up 'rpg cd' did not confirm.");
                return;
            }

            if (!await WaitForAutomatedCommandsToSettleAsync(cancellationToken))
            {
                _log.Info("[sleepy potion] Stopped: automated cooldown batch did not settle after the potion.");
                return;
            }

            _log.Info("[sleepy potion] Completed.");
        }

        private void RequestSleepyPotionStop()
        {
            if (_sleepyPotionCancellation == null || _sleepyPotionCancellation.IsCancellationRequested)
            {
                return;
            }

            _log.Info("[sleepy potion] Stop requested.");
            _sleepyPotionCancellation.Cancel();
        }

        private void SetSleepyPotionRunning(bool isRunning)
        {
            _isSleepyPotionRunning = isRunning;
            RefreshBotControlButtonColors();
        }
    }
}
