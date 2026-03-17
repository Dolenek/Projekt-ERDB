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
            if (!_botChatClient.IsReady)
            {
                _log.Info("WebView2 not ready");
                return;
            }

            var settings = GetCurrentSettings();
            await _cooldownWorkflow.RunAsync(
                _log.Info,
                settings.GetAdventureMsOrDefault(61000),
                settings.GetWorkMsOrDefault(99000),
                settings.GetFarmMsOrDefault(196000),
                settings.GetLootboxMsOrDefault(21600000));
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
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

            engine.OnCaptchaDetected += info =>
            {
                UiDispatcher.OnUI(() =>
                {
                    SelectBotTab();
                    _log.Warning("[guard] " + info);
                    _alertService.ShowCaptchaAlert(this);
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
            if (_engine != null)
            {
                await _engine.StopAsync();
            }

            _log.Engine("Engine stopped");
        }

        private async void RpgCdBtn_Click(object sender, RoutedEventArgs e)
        {
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

            var sent = await _engine.SendImmediateAsync("rpg cd");
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");
            return sent;
        }

        private BotEngine CreateEngine()
        {
            return new BotEngine(
                _botChatClient,
                GetConfiguredWorkCommand(),
                IsFarmAllowedForConfiguredArea(),
                GetConfiguredHuntMs(),
                GetConfiguredAdventureMs(),
                GetConfiguredWorkMs(),
                GetConfiguredFarmMs(),
                GetConfiguredLootboxMs());
        }

    }
}
