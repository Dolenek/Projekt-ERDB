using System;
using System.Threading.Tasks;

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
        private readonly CooldownTracker _tracker;
        private readonly LocalSettingsStore _store;

        public CooldownInitializationWorkflow(
            IDiscordChatClient chatClient,
            CooldownTracker tracker,
            LocalSettingsStore store)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
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

            await InitializeOneAsync("hunt", "rpg hunt h", 61000, setHunt, logInfo);
            await Task.Delay(BetweenCommandsMs);

            await InitializeOneAsync("adventure", "rpg adv h", adventureDefaultMs, setAdventure, logInfo);
            await Task.Delay(BetweenCommandsMs);

            await InitializeOneAsync("farm", "rpg farm", farmDefaultMs, setFarm, logInfo);
            await Task.Delay(BetweenCommandsMs);

            await InitializeOneAsync("work", "rpg chainsaw", workDefaultMs, setWork, logInfo);
            await Task.Delay(BetweenCommandsMs);

            await InitializeOneAsync("lootbox", "rpg buy ed lb", lootboxDefaultMs, setLootbox, logInfo);

            logInfo?.Invoke("Inicialize sequence finished");
        }

        private async Task InitializeOneAsync(string canonical, string action, int defaultMs, Action<int> applyValue, Action<string> logInfo)
        {
            logInfo?.Invoke($"Inicialize: {canonical} via '{action}'");

            var actionSent = await _chatClient.SendMessageAsync(action);
            if (!actionSent)
            {
                logInfo?.Invoke($"Failed to send '{action}'");
            }

            await Task.Delay(ActionDelayMs);

            var cdSent = await _chatClient.SendMessageAsync("rpg cd");
            if (!cdSent)
            {
                logInfo?.Invoke("Failed to send 'rpg cd'");
            }

            await Task.Delay(AfterCdDelayMs + ExtraLagMs);

            var lastMessage = await _chatClient.GetLastMessageTextAsync();
            if (!string.IsNullOrWhiteSpace(lastMessage))
            {
                _tracker.ApplyMessage(lastMessage);
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
        }
    }
}
