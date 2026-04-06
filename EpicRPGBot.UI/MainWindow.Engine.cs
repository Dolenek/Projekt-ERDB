using System;
using System.Windows;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async void InitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShouldBlockForWishingToken("Initialize"))
            {
                return;
            }

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

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShouldBlockForWishingToken("Start Bot"))
            {
                return;
            }

            _log.Info("Start button clicked");

            if (_engine != null && _engine.IsRunning)
            {
                _log.Info("Engine already running, Start ignored.");
                return;
            }

            await StartEngineAndRequestCooldownSnapshotAsync("Engine started (waiting for cooldown snapshot before scheduling commands)");
        }

        private void WireEngineEvents(BotEngine engine)
        {
            WireBunnyEvents(engine);

            engine.OnCommandSent += command =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Command($"Message ({command}) sent");
                    TrackSentCommandStats(command);
                });
            };

            engine.OnCommandConfirmed += (command, replySnapshot) =>
            {
                UiDispatcher.OnUI(() =>
                {
                    ApplyConfirmedCommandCooldown(command);
                });
            };

            engine.OnGuardNotification += notification =>
            {
                UiDispatcher.OnUI(() =>
                {
                    LogGuardNotification(notification);
                    ShowGuardNotification(notification);
                });
            };

            engine.OnTrainingAlert += message =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Warning("[training] " + message);
                    _alertService.ShowTrainingAlert(this, message);
                });
            };

            engine.OnMessageSeen += snapshot =>
            {
                UiDispatcher.OnUI(() => HandleObservedMessage(snapshot));
            };

            engine.OnSolverInfo += info =>
            {
                UiDispatcher.OnUI(() => _log.Info("[solver] " + info));
            };
        }

        private async void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShouldBlockForWishingToken("Stop Bot"))
            {
                return;
            }

            if (_engine != null)
            {
                await _engine.StopAsync();
            }

            _log.Engine("Engine stopped");
        }

        private async void RpgCdBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ShouldBlockForWishingToken("rpg cd"))
            {
                return;
            }

            if (_engine != null && _engine.IsRunning)
            {
                var queued = _engine.QueueCooldownSnapshotRequest();
                _log.Info(queued ? "Queued 'rpg cd' for the next legal send slot." : "'rpg cd' request already queued.");
                return;
            }

            var sent = (await _confirmedCommandSender.SendAsync("rpg cd")).IsConfirmed;
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");
        }

        private void ApplyConfirmedCommandCooldown(string command)
        {
            switch (GetTrackedCommandKey(command))
            {
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
            _engine = CreateEngine();
            WireEngineEvents(_engine);
            _engine.Start();
            _log.Engine(engineMessage);

            var sent = await _engine.SendImmediateAsync("rpg cd", _engine.ArmStartupMessageCutoff);
            await _engine.EnsureStartupMessageCutoffAsync();
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");
            return sent;
        }

        private void LogGuardNotification(Models.GuardAlertNotification notification)
        {
            if (notification == null || string.IsNullOrWhiteSpace(notification.Message))
            {
                return;
            }

            if (notification.Kind == Models.GuardAlertKind.FirstDetected)
            {
                _log.Warning("[guard] " + notification.Message);
                return;
            }

            _log.Info("[guard] " + notification.Message);
        }

        private void ShowGuardNotification(Models.GuardAlertNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            if (notification.ShouldBringToFront)
            {
                SelectBotTab();
            }

            _alertService.ShowGuardAlert(this, notification);
        }

        private BotEngine CreateEngine()
        {
            return new BotEngine(
                _botChatClient,
                GetConfiguredWorkCommand(),
                IsFarmAllowedForConfiguredArea(),
                GetConfiguredHuntMs(),
                GetConfiguredAdventureMs(),
                GetConfiguredTrainingMs(),
                GetConfiguredWorkMs(),
                GetConfiguredFarmMs(),
                GetConfiguredLootboxMs());
        }

    }
}
