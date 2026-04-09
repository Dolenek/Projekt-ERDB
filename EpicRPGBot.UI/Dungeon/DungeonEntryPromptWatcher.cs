using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonEntryPromptWatcher
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly Func<int, CancellationToken, Task> _waitAsync;

        public DungeonEntryPromptWatcher(
            IDiscordChatClient chatClient,
            Func<int, CancellationToken, Task> waitAsync)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _waitAsync = waitAsync ?? throw new ArgumentNullException(nameof(waitAsync));
        }

        public async Task<DiscordMessageSnapshot> FindVisibleEntryConfirmationPromptAsync(
            int scanCount,
            CancellationToken cancellationToken)
        {
            return FindEntryConfirmationPrompt(await _chatClient.GetRecentMessagesAsync(scanCount));
        }

        public async Task<DiscordMessageSnapshot> WaitForButtonPromptAsync(
            string buttonLabel,
            int scanCount,
            int timeoutMs,
            int pollDelayMs,
            CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(scanCount);
                var prompt = DungeonMessageInteraction.FindMessageAfter(
                    snapshots,
                    string.Empty,
                    snapshot => DungeonMessageInteraction.HasButton(snapshot, buttonLabel));
                if (prompt != null)
                {
                    return prompt;
                }

                await _waitAsync(pollDelayMs, cancellationToken);
                waitedMs += pollDelayMs;
            }

            return null;
        }

        public static DiscordMessageSnapshot FindEntryConfirmationPrompt(
            IReadOnlyList<DiscordMessageSnapshot> snapshots)
        {
            return DungeonMessageInteraction.FindMessageAfter(
                snapshots,
                string.Empty,
                snapshot => DungeonMessageInteraction.HasButton(snapshot, "yes") &&
                            (snapshot.RenderedText?.IndexOf("ARE YOU SURE YOU WANT TO ENTER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             snapshot.Text?.IndexOf("ARE YOU SURE YOU WANT TO ENTER", StringComparison.OrdinalIgnoreCase) >= 0));
        }
    }
}
