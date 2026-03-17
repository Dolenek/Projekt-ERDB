using System;
using System.Linq;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class ConfirmedCommandSender
    {
        private const int ReplyTimeoutMs = 10000;
        private const int ReplyPollDelayMs = 250;
        private const int RetryDelayMs = 1000;
        private const int MaxAttempts = 3;

        private readonly IDiscordChatClient _chatClient;

        public ConfirmedCommandSender(IDiscordChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public async Task<ConfirmedCommandSendResult> SendAsync(
            string command,
            Action<DiscordMessageSnapshot> onOutgoingRegistered = null)
        {
            DiscordMessageSnapshot lastOutgoing = null;
            var anchorMessageId = string.Empty;
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                anchorMessageId = (await _chatClient.GetLatestMessageAsync())?.Id ?? string.Empty;
                lastOutgoing = await _chatClient.SendMessageAndWaitForOutgoingAsync(command);
                if (lastOutgoing == null)
                {
                    if (attempt < MaxAttempts)
                    {
                        await Task.Delay(RetryDelayMs);
                    }

                    continue;
                }

                onOutgoingRegistered?.Invoke(lastOutgoing);

                var reply = await WaitForEpicReplyAsync(anchorMessageId, lastOutgoing.Id, command);
                if (reply != null)
                {
                    return new ConfirmedCommandSendResult(lastOutgoing, reply, attempt);
                }

                if (attempt < MaxAttempts)
                {
                    await Task.Delay(RetryDelayMs);
                }
            }

            return new ConfirmedCommandSendResult(lastOutgoing, null, MaxAttempts);
        }

        public static bool RequiresReplyConfirmation(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                   message.TrimStart().StartsWith("rpg ", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<DiscordMessageSnapshot> WaitForEpicReplyAsync(string anchorMessageId, string outgoingMessageId, string command)
        {
            var waitedMs = 0;
            while (waitedMs < ReplyTimeoutMs)
            {
                await Task.Delay(ReplyPollDelayMs);
                waitedMs += ReplyPollDelayMs;

                var reply = await _chatClient.GetEpicReplyAfterMessageAsync(outgoingMessageId);
                if (reply != null)
                {
                    return reply;
                }

                reply = await FindReplyFromRecentMessagesAsync(anchorMessageId, outgoingMessageId, command);
                if (reply != null)
                {
                    return reply;
                }
            }

            return null;
        }

        private async Task<DiscordMessageSnapshot> FindReplyFromRecentMessagesAsync(
            string anchorMessageId,
            string outgoingMessageId,
            string command)
        {
            var snapshots = await _chatClient.GetRecentMessagesAsync(20);
            if (snapshots == null || snapshots.Count == 0)
            {
                return null;
            }

            var startIndex = 0;
            if (!string.IsNullOrWhiteSpace(anchorMessageId))
            {
                var anchorIndex = snapshots.ToList().FindIndex(snapshot => string.Equals(snapshot.Id, anchorMessageId, StringComparison.Ordinal));
                if (anchorIndex >= 0)
                {
                    startIndex = anchorIndex + 1;
                }
            }

            var outgoingIndex = -1;
            for (var i = startIndex; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(outgoingMessageId) &&
                    string.Equals(snapshot.Id, outgoingMessageId, StringComparison.Ordinal))
                {
                    outgoingIndex = i;
                    break;
                }

                if (LooksLikeOutgoingCommand(snapshot, command))
                {
                    outgoingIndex = i;
                }
            }

            if (outgoingIndex < 0)
            {
                return null;
            }

            DiscordMessageSnapshot fallback = null;
            for (var i = outgoingIndex + 1; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Id))
                {
                    continue;
                }

                if (LooksLikeEpicReply(snapshot))
                {
                    return snapshot;
                }

                fallback ??= snapshot;
            }

            return fallback;
        }

        private static bool LooksLikeOutgoingCommand(DiscordMessageSnapshot snapshot, string command)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            return (snapshot.Text ?? string.Empty).IndexOf(command, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool LooksLikeEpicReply(DiscordMessageSnapshot snapshot)
        {
            var author = snapshot?.Author ?? string.Empty;
            var text = snapshot?.Text ?? string.Empty;
            return author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("successfully crafted", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("You don't have enough items to craft this", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("wait at least", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
