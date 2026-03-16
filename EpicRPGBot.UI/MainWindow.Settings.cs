using System.Windows;
using System.Windows.Controls;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void OnSettingsChanged(object sender, TextChangedEventArgs e)
        {
            PersistSettings();
        }

        private void OnFallbackChanged(object sender, RoutedEventArgs e)
        {
            PersistSettings();
        }

        private void PersistSettings()
        {
            if (_loadingSettings)
            {
                return;
            }

            _settingsStore.SetString("channel_url", ChannelUrlBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetBool("use_at_me_fallback", UseAtMeFallback.IsChecked == true);
            _settingsStore.SetString("area", AreaBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("hunt_ms", HuntCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("adventure_ms", AdventureCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("work_ms", WorkCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("farm_ms", FarmCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("lootbox_ms", LootboxCdBox.Text?.Trim() ?? string.Empty);
        }
    }
}
