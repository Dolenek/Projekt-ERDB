using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.Accounts;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private SemaphoreSlim _guildWatcherSettingsGate => CurrentAccount.GuildWatcherSettingsGate;
        private DiscordWebViewLease _guildWatcherWebViewLease { get => CurrentAccount.GuildWatcherWebViewLease; set => CurrentAccount.GuildWatcherWebViewLease = value; }
        private bool _guildRaidCoordinatorStarted { get => CurrentAccount.GuildRaidCoordinatorStarted; set => CurrentAccount.GuildRaidCoordinatorStarted = value; }

        private void HookGuildRaidSettings()
        {
            var runtime = CurrentAccount;
            runtime.GuildInfoHandler = message => RunForAccount(runtime, () => OnGuildRaidInfo(message));
            runtime.GuildGuardHandler = notification =>
                RunForAccount(runtime, () => OnGuildRaidGuardNotification(notification));
            runtime.GuildSettingsHandler = settings => ApplyGuildSettings(runtime, settings);
            _guildRaidCoordinator.OnInfo += runtime.GuildInfoHandler;
            _guildRaidCoordinator.OnGuardNotification += runtime.GuildGuardHandler;
            _settingsService.SettingsChanged += runtime.GuildSettingsHandler;
        }

        private void UnhookGuildRaidSettings()
        {
            var runtime = CurrentAccount;
            if (runtime.GuildInfoHandler != null)
                _guildRaidCoordinator.OnInfo -= runtime.GuildInfoHandler;
            if (runtime.GuildGuardHandler != null)
                _guildRaidCoordinator.OnGuardNotification -= runtime.GuildGuardHandler;
            if (runtime.GuildSettingsHandler != null)
                _settingsService.SettingsChanged -= runtime.GuildSettingsHandler;
        }

        private async void ApplyGuildSettings(AccountRuntime runtime, AppSettingsSnapshot settings)
        {
            using (UseAccount(runtime))
            {
                await OnSettingsChangedAsync(settings);
            }
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

        private async Task OnSettingsChangedAsync(AppSettingsSnapshot settings)
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

            var runtime = CurrentAccount;
            UiDispatcher.OnUI(async () =>
            {
                using (UseAccount(runtime))
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
                    await ActivateAccountAsync(runtime);
                    SelectGuildTab();
                }

                _alertService.ShowGuardAlert(
                    this, notification, bringToForeground, runtime.Definition.DisplayName);
                }
            });
        }
    }
}
