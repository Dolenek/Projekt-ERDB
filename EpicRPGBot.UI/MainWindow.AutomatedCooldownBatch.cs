using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.TimeCookie;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const int AutomatedBatchSettlePollDelayMs = 250;
        private const int AutomatedBatchSettleStablePolls = 3;
        private const int AutomatedBatchSettleTimeoutMs = 90000;

        private async Task<ConfirmedCommandSendResult> SendExclusiveEngineCommandAsync(
            string command,
            bool armStartupCutoff = false)
        {
            if (_engine == null || !_engine.IsRunning)
            {
                return new ConfirmedCommandSendResult(null, null, 0);
            }

            var result = await _engine.SendConfirmedCommandAsync(
                command,
                armStartupCutoff ? _engine.ArmStartupMessageCutoff : null);

            if (armStartupCutoff)
            {
                await _engine.EnsureStartupMessageCutoffAsync();
            }

            return result;
        }

        private async Task<bool> WaitForAutomatedCommandsToSettleAsync(CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            var stablePolls = 0;
            while (waitedMs < AutomatedBatchSettleTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (HasReadyAutomatedCommands())
                {
                    stablePolls = 0;
                }
                else
                {
                    stablePolls++;
                    if (stablePolls >= AutomatedBatchSettleStablePolls)
                    {
                        if (_engine == null || !_engine.IsRunning)
                        {
                            return false;
                        }

                        await _engine.WaitForSendLaneIdleAsync();
                        if (!HasReadyAutomatedCommands())
                        {
                            return true;
                        }

                        stablePolls = 0;
                    }
                }

                await Task.Delay(AutomatedBatchSettlePollDelayMs, cancellationToken);
                waitedMs += AutomatedBatchSettlePollDelayMs;
            }

            return false;
        }

        private bool HasReadyAutomatedCommands()
        {
            return TimeCookieLoopEvaluator.HasReadyAutomatedCommands(
                _cooldownTracker.GetTrackedSnapshot(),
                IsFarmAllowedForConfiguredArea());
        }
    }
}
