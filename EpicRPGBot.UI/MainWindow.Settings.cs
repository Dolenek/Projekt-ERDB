using System.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.CardHand;
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

        private string GetConfiguredWorkCommand()
        {
            var settings = GetCurrentSettings();
            return settings.ResolveWorkCommandForArea(settings.GetAreaOrDefault(10));
        }

        private bool IsFarmAllowedForConfiguredArea()
        {
            var settings = GetCurrentSettings();
            return settings.IsFarmAllowed(10);
        }

        private void HookAppSettings()
        {
            _settingsService.SettingsChanged += OnAppSettingsChanged;
            _cooldownTracker.RefreshWorkAliases(_settingsService.Current.WorkCommands);
        }

        private void UnhookAppSettings()
        {
            _settingsService.SettingsChanged -= OnAppSettingsChanged;
        }

        private void OnAppSettingsChanged(AppSettingsSnapshot settings)
        {
            _cooldownTracker.RefreshWorkAliases(settings?.WorkCommands);
            _engine?.UpdateCardHandSettings(settings?.CardHand);
        }

        private int GetConfiguredHuntMs()
        {
            return GetCurrentSettings().GetHuntMsOrDefault(61000);
        }

        private int GetConfiguredAdventureMs()
        {
            return GetCurrentSettings().GetAdventureMsOrDefault(61000);
        }

        private int GetConfiguredTrainingMs()
        {
            return GetCurrentSettings().GetTrainingMsOrDefault(61000);
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
            var settingsWindow = new SettingsWindow(_settingsService, LoadCardDeckFromSettingsAsync)
            {
                Owner = this
            };

            settingsWindow.ShowDialog();
        }

        private async Task<CardDeckImportResult> LoadCardDeckFromSettingsAsync()
        {
            if (!_botChatClient.IsReady)
                return new CardDeckImportResult(false, null, "Discord is not ready.");

            var result = _engine != null && _engine.IsRunning
                ? await _engine.ImportCardDeckAsync(_cardDeckImportWorkflow)
                : await _cardDeckImportWorkflow.RunAsync(
                    () => _log.Command("Message (rpg card deck) sent"),
                    CancellationToken.None);
            if (result.Success)
            {
                var cardHand = _settingsService.Current.CardHand.WithDeck(result.OwnedCards, DateTime.UtcNow);
                _settingsService.Save(_settingsService.Current.WithCardHand(cardHand));
                _log.Info("[card hand] " + result.Message);
            }
            else
            {
                _log.Warning("[card hand] " + result.Message);
                _alertService.ShowCardHandAlert(this, result.Message);
            }

            return result;
        }
    }
}
