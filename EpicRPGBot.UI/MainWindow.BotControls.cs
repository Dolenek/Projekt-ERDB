using System.Windows.Media;
using System.Windows.Controls;
using EpicRPGBot.UI.TimeCookie;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private static readonly SolidColorBrush InactiveBotControlBrush = CreateControlBrush(0x15, 0x21, 0x2C);
        private static readonly SolidColorBrush StartActiveBrush = CreateControlBrush(0x17, 0x64, 0x3A);
        private static readonly SolidColorBrush StopActiveBrush = CreateControlBrush(0x5B, 0x26, 0x30);
        private static readonly SolidColorBrush ExclusiveActiveBrush = CreateControlBrush(0x0D, 0x65, 0x73);

        private void RefreshBotControlButtonColors()
        {
            var isEngineRunning = _engine != null && _engine.IsRunning;
            StartBtn.Background = isEngineRunning ? StartActiveBrush : InactiveBotControlBrush;
            StopBtn.Background = isEngineRunning ? InactiveBotControlBrush : StopActiveBrush;
            RefreshDungeonButton();
            RefreshDuelButton();
            WishingTokenBtn.IsEnabled = (!_isTimeCookieRunning && !_isSleepyPotionRunning) || _isWishingTokenRunning;
            WishingTokenBtn.Background = _isWishingTokenRunning ? ExclusiveActiveBrush : InactiveBotControlBrush;
            RefreshSleepyPotionButton();
            RefreshTimeCookieButton(TimeCookieDungeonBtn, TimeCookieTarget.Dungeon);
            RefreshTimeCookieButton(TimeCookieDuelBtn, TimeCookieTarget.Duel);
            RefreshTimeCookieButton(TimeCookieCardHandBtn, TimeCookieTarget.CardHand);
            RefreshShellStatus();
        }

        private void RefreshDungeonButton()
        {
            if (CompleteDungeonBtn == null)
            {
                return;
            }

            var isActive = _isDungeonRunning;
            var canStart = string.IsNullOrWhiteSpace(_activeExclusiveBotOperation) || isActive;
            CompleteDungeonBtn.Content = isActive ? "Stop dungeon" : "Complete dungeon";
            CompleteDungeonBtn.IsEnabled = canStart;
            CompleteDungeonBtn.Background = isActive ? ExclusiveActiveBrush : InactiveBotControlBrush;
        }

        private void RefreshDuelButton()
        {
            if (DuelBtn == null)
            {
                return;
            }

            var canStart = string.IsNullOrWhiteSpace(_activeExclusiveBotOperation) || _isDuelRunning;
            DuelBtn.Content = _isDuelRunning ? "Stop duel" : "Start duel";
            DuelBtn.IsEnabled = canStart;
            DuelBtn.Background = _isDuelRunning ? ExclusiveActiveBrush : InactiveBotControlBrush;
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
            button.IsEnabled = !_isWishingTokenRunning &&
                !_isSleepyPotionRunning &&
                (!_isTimeCookieRunning || isActive);
            button.Background = isActive ? ExclusiveActiveBrush : InactiveBotControlBrush;
        }

        private void RefreshSleepyPotionButton()
        {
            if (SleepyPotionBtn == null)
            {
                return;
            }

            SleepyPotionBtn.Content = _isSleepyPotionRunning ? "Stop Sleepy potion" : "Sleepy potion";
            SleepyPotionBtn.IsEnabled = !_isWishingTokenRunning &&
                !_isTimeCookieRunning &&
                (!_isSleepyPotionRunning || _activeExclusiveBotOperation == SleepyPotionOperationName);
            SleepyPotionBtn.Background = _isSleepyPotionRunning ? ExclusiveActiveBrush : InactiveBotControlBrush;
        }

        private static SolidColorBrush CreateControlBrush(byte red, byte green, byte blue)
        {
            var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }
    }
}
