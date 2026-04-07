using System.Windows.Media;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private static readonly SolidColorBrush InactiveBotControlBrush = Brushes.White;
        private static readonly SolidColorBrush StartActiveBrush = Brushes.LightGreen;
        private static readonly SolidColorBrush StopActiveBrush = Brushes.LightCoral;

        private void RefreshBotControlButtonColors()
        {
            var isEngineRunning = _engine != null && _engine.IsRunning;
            StartBtn.Background = isEngineRunning ? StartActiveBrush : InactiveBotControlBrush;
            StopBtn.Background = isEngineRunning ? InactiveBotControlBrush : StopActiveBrush;
        }
    }
}
