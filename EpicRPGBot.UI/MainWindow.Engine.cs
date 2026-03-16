using System;
using System.Windows;
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

            await _cooldownWorkflow.RunAsync(
                _log.Info,
                ms => HuntCdBox.Text = ms.ToString(),
                ms => AdventureCdBox.Text = ms.ToString(),
                ms => WorkCdBox.Text = ms.ToString(),
                ms => FarmCdBox.Text = ms.ToString(),
                ms => LootboxCdBox.Text = ms.ToString(),
                SafeInt(AdventureCdBox.Text, 61000),
                SafeInt(WorkCdBox.Text, 99000),
                SafeInt(FarmCdBox.Text, 196000),
                SafeInt(LootboxCdBox.Text, 21600000));
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Start button clicked");

            if (_engine != null && _engine.IsRunning)
            {
                _log.Info("Engine already running, Start ignored.");
                return;
            }

            _engine = new BotEngine(
                _botChatClient,
                SafeInt(AreaBox.Text, 10),
                SafeInt(HuntCdBox.Text, 21000),
                SafeInt(AdventureCdBox.Text, 61000),
                SafeInt(WorkCdBox.Text, 99000),
                SafeInt(FarmCdBox.Text, 196000),
                SafeInt(LootboxCdBox.Text, 21600000));

            WireEngineEvents(_engine);

            _engine.Start();
            _log.Engine("Engine started (waiting for cooldown snapshot before scheduling commands)");

            var sent = await _engine.SendImmediateAsync("rpg cd");
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");
        }

        private void WireEngineEvents(BotEngine engine)
        {
            engine.OnCommandSent += command =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Command($"Message ({command}) sent");
                    ApplySentCommandCooldown(command);
                    if (!string.IsNullOrWhiteSpace(command) &&
                        command.Trim().StartsWith("rpg hunt", StringComparison.OrdinalIgnoreCase))
                    {
                        _huntCount++;
                        if (_huntCountText != null)
                        {
                            _huntCountText.Text = $"Hunt sent: {_huntCount}";
                        }
                    }
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

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
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

            var sent = await _botChatClient.SendMessageAsync("rpg cd");
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");
        }

        private void ApplySentCommandCooldown(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            var normalized = command.Trim().ToLowerInvariant();
            if (normalized.StartsWith("rpg hunt", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("hunt", SafeInt(HuntCdBox.Text, 61000));
            }
            else if (normalized.StartsWith("rpg adv", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("adventure", SafeInt(AdventureCdBox.Text, 61000));
            }
            else if (normalized.StartsWith("rpg farm", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("farm", SafeInt(FarmCdBox.Text, 196000));
            }
            else if (normalized.StartsWith("rpg chop", StringComparison.Ordinal) ||
                     normalized.StartsWith("rpg axe", StringComparison.Ordinal) ||
                     normalized.StartsWith("rpg bowsaw", StringComparison.Ordinal) ||
                     normalized.StartsWith("rpg chainsaw", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("work", SafeInt(WorkCdBox.Text, 99000));
            }
            else if (normalized.StartsWith("rpg buy ed lb", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("lootbox", SafeInt(LootboxCdBox.Text, 21600000));
            }
        }

    }
}
