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
            if (ShouldBlockForExclusiveBotOperation("Crafting"))
            {
                return;
            }

            var craftingWindow = new CraftingWindow(RunCraftingJobAsync)
            {
                Owner = this
            };

            craftingWindow.ShowDialog();
        }

        private async Task<CraftJobResult> RunCraftingJobAsync(CraftRequest request, Action<string> report, CancellationToken cancellationToken)
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
    }
}
