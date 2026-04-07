using System.Windows.Media;
using System.Windows.Controls;
using EpicRPGBot.UI.TimeCookie;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private static readonly SolidColorBrush InactiveBotControlBrush = Brushes.White;
        private static readonly SolidColorBrush StartActiveBrush = Brushes.LightGreen;
        private static readonly SolidColorBrush StopActiveBrush = Brushes.LightCoral;
        private static readonly SolidColorBrush ExclusiveActiveBrush = Brushes.LightSkyBlue;

        private void RefreshBotControlButtonColors()
        {
            var isEngineRunning = _engine != null && _engine.IsRunning;
            StartBtn.Background = isEngineRunning ? StartActiveBrush : InactiveBotControlBrush;
            StopBtn.Background = isEngineRunning ? InactiveBotControlBrush : StopActiveBrush;
            RefreshDungeonButton();
            WishingTokenBtn.IsEnabled = !_isTimeCookieRunning || _isWishingTokenRunning;
            WishingTokenBtn.Background = _isWishingTokenRunning ? ExclusiveActiveBrush : InactiveBotControlBrush;
            RefreshTimeCookieButton(TimeCookieDungeonBtn, TimeCookieTarget.Dungeon);
            RefreshTimeCookieButton(TimeCookieDuelBtn, TimeCookieTarget.Duel);
            RefreshTimeCookieButton(TimeCookieCardHandBtn, TimeCookieTarget.CardHand);
        }

        private void RefreshDungeonButton()
        {
            if (CompleteDungeonBtn == null)
            {
                return;
            }

            var isActive = _isDungeonRunning;
            var canStart = string.IsNullOrWhiteSpace(_activeExclusiveBotOperation) || isActive;
            CompleteDungeonBtn.Content = isActive ? "Stop Dungeon" : "Complete Dungeon";
            CompleteDungeonBtn.IsEnabled = canStart;
            CompleteDungeonBtn.Background = isActive ? ExclusiveActiveBrush : InactiveBotControlBrush;
        }

        private void RefreshTimeCookieButton(Button button, TimeCookieTarget target)
        {
            if (button == null)
            {
                return;
            }

            var definition = TimeCookieTargetCatalog.Get(target);
            var isActive = _isTimeCookieRunning && _activeTimeCookieTarget == target;
            button.Content = isActive ? $"Stop {definition.DisplayName}" : definition.DisplayName;
            button.IsEnabled = !_isWishingTokenRunning && (!_isTimeCookieRunning || isActive);
            button.Background = isActive ? ExclusiveActiveBrush : InactiveBotControlBrush;
        }
    }
}
