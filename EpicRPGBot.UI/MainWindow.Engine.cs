using System;
using System.Windows;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async void InitBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
                var browserLease = await AcquireBotWorkflowAsync();
                try
                {
                    if (ShouldBlockForExclusiveBotOperation("Initialize")) return;
                    if (!_botChatClient.IsReady)
                    {
                        _log.Info("WebView2 not ready");
                        return;
                    }

                    var settings = GetCurrentSettings();
                    await _cooldownWorkflow.RunAsync(
                        _log.Info,
                        settings.GetAdventureMsOrDefault(61000),
                        settings.GetTrainingMsOrDefault(61000),
                        settings.GetWorkMsOrDefault(99000),
                        settings.GetFarmMsOrDefault(196000),
                        settings.GetLootboxMsOrDefault(21600000));
                }
                finally
                {
                    await browserLease.ReleaseAsync();
                }
            }
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
                if (ShouldBlockForExclusiveBotOperation("Start Bot")) return;
                _log.Info("Start button clicked");
                if (_engine != null && _engine.IsRunning)
                {
                    _log.Info("Engine already running, Start ignored.");
                    return;
                }

                try
                {
                    await StartEngineAndRequestCooldownSnapshotAsync(
                        "Engine started (waiting for cooldown snapshot before scheduling commands)");
                }
                catch (Exception ex)
                {
                    await SetEngineBrowserDemandAsync(false);
                    _log.Warning("Engine could not start: " + ex.Message);
                    SetDiscordStatus("Error", "DangerBrush");
                }
            }
        }

        private void WireEngineEvents(BotEngine engine, Accounts.AccountRuntime runtime)
        {
            WireBunnyEvents(engine, runtime);
            WireEngineLifecycleEvents(engine, runtime);
            WireEngineCommandEvents(engine, runtime);
            WireEngineAlertEvents(engine, runtime);
            engine.OnMessageSeen += snapshot =>
                DispatchAccount(runtime, () => HandleObservedMessage(snapshot));
            engine.OnSolverInfo += (message, reference) =>
                DispatchAccount(runtime, () => _log.Info("[solver] " + message, reference));
        }

        private void WireEngineLifecycleEvents(BotEngine engine, Accounts.AccountRuntime runtime)
        {
            engine.OnEngineStarted += () => DispatchAccount(runtime, () =>
            {
                RefreshBotControlButtonColors();
                ReconcileMessagePolling();
            });
            engine.OnEngineStopped += () => DispatchAccount(runtime, () =>
            {
                RefreshBotControlButtonColors();
                ReconcileMessagePolling();
            });
        }

        private void WireEngineCommandEvents(BotEngine engine, Accounts.AccountRuntime runtime)
        {
            engine.OnCommandSent += (command, snapshot) => DispatchAccount(runtime, () =>
            {
                _log.Command($"Message ({command}) sent", DiscordMessageReference.FromSnapshot(snapshot));
                TrackSentCommandStats(command);
            });
            engine.OnCommandConfirmed += (command, reply) =>
                DispatchAccount(runtime, () => ApplyConfirmedCommandCooldown(command));
        }

        private void WireEngineAlertEvents(BotEngine engine, Accounts.AccountRuntime runtime)
        {
            engine.OnGuardNotification += notification => DispatchAccount(runtime, () =>
            {
                LogGuardNotification(notification);
                ShowGuardNotification(notification);
            });
            engine.OnTrainingAlert += (message, reference) => DispatchAccount(runtime, () =>
            {
                _log.Warning("[training] " + message, reference);
                _alertService.ShowTrainingAlert(this, $"{runtime.Definition.DisplayName}: {message}");
            });
            engine.OnCardHandInfo += (message, reference) =>
                DispatchAccount(runtime, () => _log.Info("[card hand] " + message, reference));
            engine.OnCardHandAlert += (message, reference) => DispatchAccount(runtime, () =>
            {
                _log.Warning("[card hand] " + message, reference);
                _alertService.ShowCardHandAlert(this, $"{runtime.Definition.DisplayName}: {message}");
            });
        }

        private void DispatchAccount(Accounts.AccountRuntime runtime, Action action)
        {
            UiDispatcher.OnUI(() => RunForAccount(runtime, action));
        }

        private async void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
                if (ShouldBlockForExclusiveBotOperation("Stop Bot")) return;
                if (_engine != null) await _engine.StopAsync();
                await SetEngineBrowserDemandAsync(false);
                _log.Engine("Engine stopped");
            }
        }

        private async void RpgCdBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
                var browserLease = await AcquireBotWorkflowAsync();
                try
                {
                    if (ShouldBlockForExclusiveBotOperation("rpg cd")) return;
                    if (_engine != null && _engine.IsRunning)
                    {
                        var queued = _engine.QueueCooldownSnapshotRequest();
                        _log.Info(queued
                            ? "Queued 'rpg cd' for the next legal send slot."
                            : "'rpg cd' request already queued.");
                        return;
                    }

                    var result = await _confirmedCommandSender.SendAsync("rpg cd");
                    _log.Info(
                        result.IsConfirmed ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.",
                        DiscordMessageReference.FromSnapshot(result.OutgoingMessage));
                }
                finally
                {
                    await browserLease.ReleaseAsync();
                }
            }
        }

        private void ApplyConfirmedCommandCooldown(string command)
        {
            switch (GetTrackedCommandKey(command))
            {
                case "daily":
                    _cooldownTracker.SetCooldown("daily", 86400000);
                    break;
                case "weekly":
                    _cooldownTracker.SetCooldown("weekly", 604800000);
                    break;
                case "card_hand":
                    _cooldownTracker.SetCooldown("card_hand", 86400000);
                    break;
                case "hunt":
                    _cooldownTracker.SetCooldown("hunt", GetConfiguredHuntMs());
                    break;
                case "adventure":
                    _cooldownTracker.SetCooldown("adventure", GetConfiguredAdventureMs());
                    break;
                case "training":
                    _cooldownTracker.SetCooldown("training", GetConfiguredTrainingMs());
                    break;
                case "farm":
                    _cooldownTracker.SetCooldown("farm", GetConfiguredFarmMs());
                    break;
                case "work":
                    _cooldownTracker.SetCooldown("work", GetConfiguredWorkMs());
                    break;
                case "lootbox":
                    _cooldownTracker.SetCooldown("lootbox", GetConfiguredLootboxMs());
                    break;
            }
        }

        private async Task<bool> StartEngineAndRequestCooldownSnapshotAsync(string engineMessage)
        {
            await StartEngineAsync(engineMessage);

            var sent = await _engine.SendImmediateAsync("rpg cd", _engine.ArmStartupMessageCutoff);
            await _engine.EnsureStartupMessageCutoffAsync();
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");
            return sent;
        }

        private async Task StartEngineAsync(string engineMessage)
        {
            await SetEngineBrowserDemandAsync(true);
            _engine = CreateEngine();
            WireEngineEvents(_engine, CurrentAccount);
            _engine.Start();
            _log.Engine(engineMessage);
        }

        private void LogGuardNotification(Models.GuardAlertNotification notification)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.Message))
            {
                return;
            }

            if (notification.Kind == Models.GuardAlertKind.FirstDetected)
            {
                _log.Warning("[guard] " + notification.Message, notification.MessageReference);
                return;
            }

            _log.Info("[guard] " + notification.Message, notification.MessageReference);
        }

        private void ShowGuardNotification(Models.GuardAlertNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            var account = CurrentAccount;
            var bringToForeground = ShouldBringGuardAlertToForeground(notification);
            if (bringToForeground)
            {
                _ = ActivateAccountAndSelectTabAsync(account, DiscordTabRole.Bot);
            }

            _alertService.ShowGuardAlert(
                this, notification, bringToForeground, account.Definition.DisplayName);
        }

        private BotEngine CreateEngine()
        {
            var runtime = CurrentAccount;
            return new BotEngine(
                runtime.BotChatClient,
                GetConfiguredWorkCommand(),
                IsFarmAllowedForConfiguredArea(),
                GetConfiguredHuntMs(),
                GetConfiguredAdventureMs(),
                GetConfiguredTrainingMs(),
                GetConfiguredWorkMs(),
                GetConfiguredFarmMs(),
                GetConfiguredLootboxMs(),
                () => runtime.SettingsService.Current.CardHand);
        }

    }
}
