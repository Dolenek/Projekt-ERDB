using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void CraftingBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            if (ShouldBlockForExclusiveBotOperation("Crafting"))
            {
                return;
            }

            if (!TryBeginExclusiveBotOperation("Crafting")) return;

            try
            {
                var craftingWindow = new CraftingWindow((request, report, cancellationToken) =>
                    RunCraftingJobForAccountAsync(account, request, report, cancellationToken))
                {
                    Owner = this
                };
                craftingWindow.ShowDialog();
            }
            finally
            {
                using (UseAccount(account)) EndExclusiveBotOperation("Crafting");
            }
        }

        private async Task<CraftJobResult> RunCraftingJobForAccountAsync(
            Accounts.AccountRuntime account,
            CraftRequest request,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            using (UseAccount(account))
            {
                return await RunCraftingJobAsync(request, report, cancellationToken);
            }
        }

        private async Task<CraftJobResult> RunCraftingJobAsync(CraftRequest request, Action<string> report, CancellationToken cancellationToken)
        {
            var browserLease = await AcquireBotWorkflowAsync();
            try
            {
                var shouldResumeEngine = _engine != null && _engine.IsRunning;
                if (shouldResumeEngine)
                {
                    report?.Invoke("Pausing bot automation.");
                    await _engine.StopAsync();
                    _log.Engine("Engine paused for crafting");
                }

                try
                {
                    return await _logCraftingWorkflow.RunAsync(request, message =>
                    {
                        report?.Invoke(message);
                        _log.Info("[craft] " + message);
                    }, cancellationToken);
                }
                finally
                {
                    if (shouldResumeEngine)
                    {
                        report?.Invoke("Resuming bot automation.");
                        await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed after crafting");
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
