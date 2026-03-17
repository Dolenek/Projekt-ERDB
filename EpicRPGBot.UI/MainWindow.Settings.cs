using System.Windows;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Settings;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private AppSettingsSnapshot GetCurrentSettings()
        {
            return _settingsService.Current;
        }

        private int GetConfiguredArea()
        {
            return GetCurrentSettings().GetAreaOrDefault(10);
        }

        private int GetConfiguredHuntMs()
        {
            return GetCurrentSettings().GetHuntMsOrDefault(61000);
        }

        private int GetConfiguredAdventureMs()
        {
            return GetCurrentSettings().GetAdventureMsOrDefault(61000);
        }

        private int GetConfiguredWorkMs()
        {
            return GetCurrentSettings().GetWorkMsOrDefault(99000);
        }

        private int GetConfiguredFarmMs()
        {
            return GetCurrentSettings().GetFarmMsOrDefault(196000);
        }

        private int GetConfiguredLootboxMs()
        {
            return GetCurrentSettings().GetLootboxMsOrDefault(21600000);
        }

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settingsService)
            {
                Owner = this
            };

            settingsWindow.ShowDialog();
        }
    }
}
