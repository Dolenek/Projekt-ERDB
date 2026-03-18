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
            if (ShouldBlockForWishingToken("Dismantle"))
            {
                return;
            }

            var dismantleWindow = new DismantleWindow(RunDismantlingJobAsync)
            {
                Owner = this
            };

            dismantleWindow.ShowDialog();
        }

        private async Task<CraftJobResult> RunDismantlingJobAsync(DismantleRequest request, Action<string> report, CancellationToken cancellationToken)
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
    }
}
