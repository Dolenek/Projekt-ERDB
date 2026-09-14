using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Dismantling;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void DismantleBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            if (ShouldBlockForExclusiveBotOperation("Dismantle"))
            {
                return;
            }

            if (!TryBeginExclusiveBotOperation("Dismantle")) return;

            try
            {
                var dismantleWindow = new DismantleWindow((request, report, cancellationToken) =>
                    RunDismantlingJobForAccountAsync(account, request, report, cancellationToken))
                {
                    Owner = this
                };
                dismantleWindow.ShowDialog();
            }
            finally
            {
                using (UseAccount(account)) EndExclusiveBotOperation("Dismantle");
            }
        }

        private async Task<CraftJobResult> RunDismantlingJobForAccountAsync(
            Accounts.AccountRuntime account,
            DismantleRequest request,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            using (UseAccount(account))
            {
                return await RunDismantlingJobAsync(request, report, cancellationToken);
            }
        }

        private async Task<CraftJobResult> RunDismantlingJobAsync(DismantleRequest request, Action<string> report, CancellationToken cancellationToken)
        {
            var browserLease = await AcquireBotWorkflowAsync();
            try
            {
                var shouldResumeEngine = _engine != null && _engine.IsRunning;
                if (shouldResumeEngine)
                {
                    report?.Invoke("Pausing bot automation.");
                    await _engine.StopAsync();
                    _log.Engine("Engine paused for dismantling");
                }

                try
                {
                    return await _dismantlingWorkflow.RunAsync(request, message =>
                    {
                        report?.Invoke(message);
                        _log.Info("[dismantle] " + message);
                    }, cancellationToken);
                }
                finally
                {
                    if (shouldResumeEngine)
                    {
                        report?.Invoke("Resuming bot automation.");
                        await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed after dismantling");
                    }
                }
            }
            finally
            {
                await ReleaseStoppedEngineDemandAsync();
                await browserLease.ReleaseAsync();
            }
        }
    }
}
