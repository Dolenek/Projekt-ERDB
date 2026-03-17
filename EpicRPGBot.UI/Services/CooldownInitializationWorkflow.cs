using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class CooldownInitializationWorkflow
    {
        private const int ActionDelayMs = 2000;
        private const int AfterCdDelayMs = 1000;
        private const int ExtraLagMs = 1000;
        private const int OverheadMs = ActionDelayMs + AfterCdDelayMs + ExtraLagMs;
        private const int BetweenCommandsMs = 3000;

        private readonly IDiscordChatClient _chatClient;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly CooldownTracker _tracker;
        private readonly LocalSettingsStore _store;

        public CooldownInitializationWorkflow(
            IDiscordChatClient chatClient,
            CooldownTracker tracker,
            LocalSettingsStore store)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _confirmedCommandSender = new ConfirmedCommandSender(_chatClient);
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task RunAsync(
            Action<string> logInfo,
            Action<int> setHunt,
            Action<int> setAdventure,
            Action<int> setWork,
            Action<int> setFarm,
            Action<int> setLootbox,
            int adventureDefaultMs,
            int workDefaultMs,
            int farmDefaultMs,
            int lootboxDefaultMs)
        {
            logInfo?.Invoke("Inicialize sequence started");

            var openingSnapshot = await CaptureOpeningSnapshotAsync(logInfo);
            if (openingSnapshot == null)
            {
                logInfo?.Invoke("Inicialize aborted: failed to parse the opening 'rpg cd' snapshot.");
                return;
            }

            var steps = new[]
            {
                new InitializationStep("hunt", "rpg hunt h", 61000, setHunt),
                new InitializationStep("adventure", "rpg adv h", adventureDefaultMs, setAdventure),
                new InitializationStep("farm", "rpg farm", farmDefaultMs, setFarm),
                new InitializationStep("work", "rpg chainsaw", workDefaultMs, setWork),
                new InitializationStep("lootbox", "rpg buy ed lb", lootboxDefaultMs, setLootbox)
            };

            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (HasRemainingCooldown(openingSnapshot, step.Canonical))
                {
                    logInfo?.Invoke($"Inicialize: {step.Canonical} skipped (already on cooldown in opening snapshot)");
                    continue;
                }

                var initialized = await InitializeOneAsync(step.Canonical, step.Action, step.DefaultMs, step.ApplyValue, logInfo);
                if (initialized && i < steps.Length - 1)
                {
                    await Task.Delay(BetweenCommandsMs);
                }
            }

            logInfo?.Invoke("Inicialize sequence finished");
        }

        private async Task<TrackedCooldownSnapshot> CaptureOpeningSnapshotAsync(Action<string> logInfo)
        {
            var result = await _confirmedCommandSender.SendAsync("rpg cd");
            if (!result.IsConfirmed || !_tracker.ApplyMessage(result.ReplyMessage?.Text))
            {
                logInfo?.Invoke("Inicialize: opening 'rpg cd' snapshot was not usable.");
                return null;
            }

            return _tracker.GetTrackedSnapshot();
        }

        private async Task<bool> InitializeOneAsync(string canonical, string action, int defaultMs, Action<int> applyValue, Action<string> logInfo)
        {
            logInfo?.Invoke($"Inicialize: {canonical} via '{action}'");

            var actionResult = await _confirmedCommandSender.SendAsync(action);
            if (!actionResult.IsConfirmed)
            {
                logInfo?.Invoke($"Inicialize: failed to send '{action}'");
                return false;
            }

            if (LooksLikeCooldownReply(actionResult.ReplyMessage?.Text))
            {
                logInfo?.Invoke($"Inicialize: {canonical} skipped (command reply reported an active cooldown)");
                return false;
            }

            await Task.Delay(ActionDelayMs);

            var cdResult = await _confirmedCommandSender.SendAsync("rpg cd");
            if (!cdResult.IsConfirmed)
            {
                logInfo?.Invoke("Inicialize: failed to refresh 'rpg cd' after action.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(cdResult.ReplyMessage?.Text))
            {
                _tracker.ApplyMessage(cdResult.ReplyMessage.Text);
            }

            var baseMs = defaultMs;
            var remaining = _tracker.GetRemaining(canonical);
            if (remaining.HasValue)
            {
                baseMs = (int)Math.Max(0, remaining.Value.TotalMilliseconds) + OverheadMs;
            }

            _store.SetString($"{canonical}_ms", baseMs.ToString());
            applyValue?.Invoke(baseMs);
            logInfo?.Invoke($"Inicialize: {canonical} cooldown set to {baseMs} ms (saved)");
            return true;
        }

        private static bool HasRemainingCooldown(TrackedCooldownSnapshot snapshot, string canonical)
        {
            var remaining = canonical == "hunt" ? snapshot.Hunt :
                canonical == "adventure" ? snapshot.Adventure :
                canonical == "work" ? snapshot.Work :
                canonical == "farm" ? snapshot.Farm :
                canonical == "lootbox" ? snapshot.Lootbox :
                null;

            return remaining.HasValue && remaining.Value > TimeSpan.Zero;
        }

        private static bool LooksLikeCooldownReply(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                   message.IndexOf("wait at least", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class InitializationStep
        {
            public InitializationStep(string canonical, string action, int defaultMs, Action<int> applyValue)
            {
                Canonical = canonical;
                Action = action;
                DefaultMs = defaultMs;
                ApplyValue = applyValue;
            }

            public string Canonical { get; }
            public string Action { get; }
            public int DefaultMs { get; }
            public Action<int> ApplyValue { get; }
        }
    }
}
