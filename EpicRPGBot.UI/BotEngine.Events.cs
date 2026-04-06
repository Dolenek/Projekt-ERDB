using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private void EventCheck(Models.DiscordMessageSnapshot snapshot)
        {
            var msg = snapshot?.Text ?? string.Empty;
            HandleGuardMessage(msg);
            if (TryHandleTrainingPrompt(snapshot))
            {
                _previousMessageText = msg;
                return;
            }

            if (msg.IndexOf("TEST", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("I hear you: " + DateTime.Now);
            }
            else if (msg.IndexOf("BOT HELP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("Change work - chop / axe / bowsaw / chainsaw ");
                _ = SendAndEmitAsync("Change farm - farm / potato / carrot / bread");
                _ = SendAndEmitAsync("bot farming - will start farming");
            }
            else if (msg.IndexOf("STOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _scheduler.StopAll();
                _scheduler.ClearPending();
            }
            else if (msg.IndexOf("START", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _awaitingStartupCooldownSnapshot = true;
                _ = SendAndEmitAsync("rpg cd");
                _checkMessageTimer.Start();
            }
            else if (msg.IndexOf("CHANGE WORK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                HandleChangeWork(msg);
            }
            else if (msg.IndexOf("CHANGE FARM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                HandleChangeFarm(msg);
            }
            else if (msg.IndexOf("BOT FARMING", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("I am farming");
                _scheduler.Schedule(TrackedCommandKind.Farm, TimeSpan.FromMilliseconds(_farmCooldown), _running);
            }
            else if (msg.IndexOf("You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("RUN");
            }
            else if (msg.IndexOf("megarace boost", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("yes");
            }
            else if (msg.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("CUT");
            }
            else if (msg.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("LURE");
            }
            else if (msg.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("CATCH");
            }
            else if (msg.IndexOf("God accidentally dropped an EPIC coin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg, "I SHALL BRING THE EPIC TO THE COIN", "MY PRECIOUS", "WHAT IS EPIC? THIS COIN", "YES! AN EPIC COIN", "OPERATION: EPIC COIN");
            }
            else if (msg.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg, "BACK OFF THIS IS MINE!!", "HACOINA MATATA", "THIS IS MINE", "ALL THE COINS BELONG TO ME", "GIMME DA MONEY", "OPERATION: COINS");
            }
            else if (msg.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg, "YUP I WILL DO THAT", "I WANT THAT", "HEY EPIC NPC! I WANT TO TRADE WITH YOU", "THAT SOUNDS LIKE AN OP BUSINESS", "OWO ME!!!");
            }
            else if (msg.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("SUMMON");
            }
            else if (msg.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("TIME TO FIGHT");
            }

            _previousMessageText = msg;
        }

        private void HandleGuardMessage(string message)
        {
            if (GuardIncidentTracker.ContainsGuardClear(message))
            {
                var cleared = _guardIncidentTracker.ClearIfActive();
                if (cleared != null)
                {
                    _captchaSolver.CancelCurrentSolve();
                    CompleteGuardSolve(_activeGuardMessageId);
                    _scheduler.ResumeAll(_running);
                    OnGuardNotification?.Invoke(cleared);
                    ReportSolverInfo(cleared.Message);
                }

                return;
            }

            var latestHasGuard = GuardIncidentTracker.ContainsGuardPrompt(message);
            var previousHasGuard = GuardIncidentTracker.ContainsGuardPrompt(_previousMessageText);
            if (!latestHasGuard && !previousHasGuard)
            {
                return;
            }

            var targetMessageId = ResolveGuardTargetMessageId(latestHasGuard, previousHasGuard);
            var detectionInfo = latestHasGuard
                ? "Captcha detected in latest message."
                : "Captcha detected in previous message.";
            var notification = _guardIncidentTracker.RegisterDetection(detectionInfo);
            if (notification != null)
            {
                OnGuardNotification?.Invoke(notification);
            }

            ReportSolverInfo(detectionInfo);
            if (!TryBeginGuardSolve(targetMessageId))
            {
                return;
            }

            _ = SolveCaptchaAsync(targetMessageId);
        }

        private void HandleChangeWork(string message)
        {
            if (message.IndexOf("CHOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg chop";
                _ = SendAndEmitAsync("I am chopping treez");
            }
            else if (message.IndexOf("AXE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg axe";
                _ = SendAndEmitAsync("I am using an axe");
            }
            else if (message.IndexOf("BOWSAW", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg bowsaw";
                _ = SendAndEmitAsync("I am using a bowsaw");
            }
            else if (message.IndexOf("CHAINSAW", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg chainsaw";
                _ = SendAndEmitAsync("I am using a chainsaw");
            }
        }

        private void HandleChangeFarm(string message)
        {
            if (message.IndexOf("FARM FARM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm";
                _ = SendAndEmitAsync("I am farming normally");
            }
            else if (message.IndexOf("CARROT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm carrot";
                _ = SendAndEmitAsync("I am farming carrots");
            }
            else if (message.IndexOf("POTATO", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm potato";
                _ = SendAndEmitAsync("I am farming potatoes");
            }
            else if (message.IndexOf("BREAD", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm bread";
                _ = SendAndEmitAsync("I am farming bread");
            }
        }

        private void RespondFirstPresent(string message, params string[] options)
        {
            if (string.IsNullOrEmpty(message) || options == null || options.Length == 0)
            {
                return;
            }

            foreach (var option in options)
            {
                if (string.IsNullOrEmpty(option) || message.IndexOf(option, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                _ = SendRawWithGlobalCooldownAsync(option);
                return;
            }
        }

        private async Task SolveCaptchaAsync(string targetMessageId)
        {
            try
            {
                await _captchaSolver.TrySolveAsync(
                    targetMessageId,
                    _lastMessageId,
                    _previousMessageId,
                    SendAndEmitAsync,
                    _scheduler.PauseAll,
                    () => _scheduler.ResumeAll(_running),
                    ReportSolverInfo);
            }
            finally
            {
                CompleteGuardSolve(targetMessageId);
            }
        }

        private void ReportSolverInfo(string info)
        {
            OnSolverInfo?.Invoke(info);
        }
        private static Task SafeDelay(int milliseconds, CancellationToken cancellationToken)
        {
            return Task.Delay(milliseconds, cancellationToken);
        }
    }
}
