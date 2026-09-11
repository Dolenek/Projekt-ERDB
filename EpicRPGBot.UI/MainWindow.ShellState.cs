using System.Windows.Media;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private string _discordStatus = "Initializing";
        private string _discordStatusBrushKey = "WarningBrush";

        private void SetDiscordStatus(string status, string brushKey)
        {
            _discordStatus = status;
            _discordStatusBrushKey = brushKey;
            RefreshShellStatus();
        }

        private void RefreshShellStatus()
        {
            var discordBrush = FindStatusBrush(_discordStatusBrushKey);
            if (ConnectionStatusText != null)
            {
                ConnectionStatusText.Text = _discordStatus;
            }

            if (ConnectionStatusDot != null)
            {
                ConnectionStatusDot.Fill = discordBrush;
            }

            var engineRunning = _engine != null && _engine.IsRunning;
            var workflowRunning = !string.IsNullOrWhiteSpace(_activeExclusiveBotOperation);
            var engineStatus = workflowRunning
                ? _activeExclusiveBotOperation
                : engineRunning ? "Running" : "Stopped";
            var engineBrush = FindStatusBrush(
                workflowRunning ? "AccentBrush" : engineRunning ? "SuccessBrush" : "MutedTextBrush");

            if (EngineStatusText != null)
            {
                EngineStatusText.Text = engineStatus;
            }

            if (EngineStatusDot != null)
            {
                EngineStatusDot.Fill = engineBrush;
            }

        }

        private Brush FindStatusBrush(string resourceKey)
        {
            return TryFindResource(resourceKey) as Brush ?? Brushes.Gray;
        }
    }
}
