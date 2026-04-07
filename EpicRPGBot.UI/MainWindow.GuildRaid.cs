using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void HookGuildRaidSettings()
        {
            _guildRaidCoordinator.OnInfo += OnGuildRaidInfo;
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        private void UnhookGuildRaidSettings()
        {
            _guildRaidCoordinator.OnInfo -= OnGuildRaidInfo;
            _settingsService.SettingsChanged -= OnSettingsChanged;
        }

        private async Task StartGuildRaidWatcherAsync()
        {
            try
            {
                await _guildRaidCoordinator.StartAsync();
            }
            catch (Exception ex)
            {
                _log.Warning("[guild] Failed to start watcher: " + ex.Message);
            }
        }

        private async void OnSettingsChanged(AppSettingsSnapshot settings)
        {
            try
            {
                await _guildRaidCoordinator.ApplySettingsAsync(settings);
            }
            catch (Exception ex)
            {
                _log.Warning("[guild] Failed to apply settings: " + ex.Message);
            }
        }

        private void OnGuildRaidInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            _log.Info("[guild] " + message);
        }
    }
}
