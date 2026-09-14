using System.Windows;
using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Settings;
using EpicRPGBot.UI.WorkCommands;

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

        private bool ShouldBringGuardAlertToForeground(GuardAlertNotification notification)
        {
            return notification?.ShouldBringToFront == true &&
                   GetCurrentSettings().BringGuardAlertsToForeground;
        }

        private void HookAppSettings()
        {
            var runtime = CurrentAccount;
            runtime.AppSettingsHandler = settings =>
                RunForAccount(runtime, () => OnAppSettingsChanged(settings));
            _settingsService.SettingsChanged += runtime.AppSettingsHandler;
            _cooldownTracker.RefreshWorkAliases(_settingsService.Current.WorkCommands);
        }

        private void UnhookAppSettings()
        {
            var runtime = CurrentAccount;
            if (runtime.AppSettingsHandler != null)
                _settingsService.SettingsChanged -= runtime.AppSettingsHandler;
        }

        private void OnAppSettingsChanged(AppSettingsSnapshot settings)
        {
            _cooldownTracker.RefreshWorkAliases(settings?.WorkCommands);
            _engine?.UpdateCardHandSettings(settings?.CardHand);
            if (settings != null)
            {
                _engine?.UpdateWorkCommand(
                    settings.ResolveWorkCommandForArea(settings.GetAreaOrDefault(10)));
            }
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
            var account = _activeAccountRuntime;
            var settingsWindow = new SettingsWindow(
                account.SettingsService,
                () => LoadCardDeckForAccountAsync(account),
                cancellationToken => LoadAutoBestWorkCommandsForAccountAsync(account, cancellationToken))
            {
                Owner = this
            };

            settingsWindow.ShowDialog();
        }

        private async Task<CardDeckImportResult> LoadCardDeckForAccountAsync(
            Accounts.AccountRuntime account)
        {
            using (UseAccount(account))
            {
                return await LoadCardDeckFromSettingsAsync();
            }
        }

        private async Task<AutoBestWorkCommandResult> LoadAutoBestWorkCommandsForAccountAsync(
            Accounts.AccountRuntime account,
            CancellationToken cancellationToken)
        {
            using (UseAccount(account))
            {
                return await LoadAutoBestWorkCommandsAsync(cancellationToken);
            }
        }

        private async Task<AutoBestWorkCommandResult> LoadAutoBestWorkCommandsAsync(
            CancellationToken cancellationToken)
        {
            var browserLease = await AcquireBotWorkflowAsync();
            try
            {
                if (!_botChatClient.IsReady)
                {
                    return AutoBestWorkCommandResult.Failure("Discord is not ready.");
                }

                var ascended = _settingsService.Current.Ascended;
                var result = _engine != null && _engine.IsRunning
                    ? await _engine.LoadAutoBestWorkCommandsAsync(
                        _autoBestWorkCommandWorkflow,
                        ascended,
                        cancellationToken)
                    : await _autoBestWorkCommandWorkflow.RunAsync(
                        ascended,
                        LogAutoBestOutgoingCommand,
                        cancellationToken);
                if (result.Success)
                    _log.Info("[work commands] " + result.Message);
                else
                    _log.Warning("[work commands] " + result.Message);
                return result;
            }
            finally
            {
                await browserLease.ReleaseAsync();
            }
        }

        private void LogAutoBestOutgoingCommand(
            string command,
            DiscordMessageSnapshot snapshot)
        {
            _log.Command(
                $"Message ({command}) sent",
                DiscordMessageReference.FromSnapshot(snapshot));
        }

        private async Task<CardDeckImportResult> LoadCardDeckFromSettingsAsync()
        {
            var browserLease = await AcquireBotWorkflowAsync();
            try
            {
                if (!_botChatClient.IsReady)
                    return new CardDeckImportResult(false, null, "Discord is not ready.");

                var result = _engine != null && _engine.IsRunning
                    ? await _engine.ImportCardDeckAsync(_cardDeckImportWorkflow)
                    : await _cardDeckImportWorkflow.RunAsync(
                        snapshot => _log.Command(
                            "Message (rpg card deck) sent",
                            Models.DiscordMessageReference.FromSnapshot(snapshot)),
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
                    _alertService.ShowCardHandAlert(
                        this, $"{CurrentAccount.Definition.DisplayName}: {result.Message}");
                }

                return result;
            }
            finally
            {
                await browserLease.ReleaseAsync();
            }
        }
    }
}
