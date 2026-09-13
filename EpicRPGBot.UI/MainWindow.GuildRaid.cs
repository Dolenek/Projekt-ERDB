using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private readonly SemaphoreSlim _guildWatcherSettingsGate = new SemaphoreSlim(1, 1);
        private DiscordWebViewLease _guildWatcherWebViewLease;
        private bool _guildRaidCoordinatorStarted;

        private void HookGuildRaidSettings()
        {
            _guildRaidCoordinator.OnInfo += OnGuildRaidInfo;
            _guildRaidCoordinator.OnGuardNotification += OnGuildRaidGuardNotification;
            _settingsService.SettingsChanged += OnSettingsChanged;
        }

        private void UnhookGuildRaidSettings()
        {
            _guildRaidCoordinator.OnInfo -= OnGuildRaidInfo;
            _guildRaidCoordinator.OnGuardNotification -= OnGuildRaidGuardNotification;
            _settingsService.SettingsChanged -= OnSettingsChanged;
        }

        private async Task StartGuildRaidWatcherAsync()
        {
            try
            {
                await ReconcileGuildRaidWatcherAsync(GetCurrentSettings());
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
                await ReconcileGuildRaidWatcherAsync(settings);
            }
            catch (Exception ex)
            {
                _log.Warning("[guild] Failed to apply settings: " + ex.Message);
            }
        }

        private async Task ReconcileGuildRaidWatcherAsync(AppSettingsSnapshot settings)
        {
            await _guildWatcherSettingsGate.WaitAsync();
            try
            {
                var shouldRun = GuildRaidWatcherPolicy.ShouldRun(settings);
                if (shouldRun && _guildWatcherWebViewLease == null)
                {
                    _guildWatcherWebViewLease = await _guildWebViewSession.AcquireAsync(
                        DiscordWebViewActivityReason.GuildWatcher);
                }

                if (_guildRaidCoordinatorStarted)
                {
                    await _guildRaidCoordinator.ApplySettingsAsync(settings);
                }
                else
                {
                    await _guildRaidCoordinator.StartAsync();
                    _guildRaidCoordinatorStarted = true;
                }

                if (!shouldRun)
                {
                    await ReleaseGuildWatcherWebViewAsync();
                }
            }
            finally
            {
                _guildWatcherSettingsGate.Release();
            }
        }

        private async Task ReleaseGuildWatcherWebViewAsync()
        {
            var lease = _guildWatcherWebViewLease;
            _guildWatcherWebViewLease = null;
            if (lease != null)
            {
                await lease.ReleaseAsync();
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

        private void OnGuildRaidGuardNotification(GuardAlertNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            UiDispatcher.OnUI(() =>
            {
                if (notification.Kind == GuardAlertKind.FirstDetected)
                {
                    _log.Warning("[guild][guard] " + notification.Message);
                }
                else
                {
                    _log.Info("[guild][guard] " + notification.Message);
                }

                var bringToForeground = ShouldBringGuardAlertToForeground(notification);
                if (bringToForeground)
                {
                    SelectGuildTab();
                }

                _alertService.ShowGuardAlert(this, notification, bringToForeground);
            });
        }
    }
}
