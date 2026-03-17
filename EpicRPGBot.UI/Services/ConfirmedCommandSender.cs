using System;
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

                var reply = await WaitForEpicReplyAsync(anchorMessageId, command);
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

        private async Task<DiscordMessageSnapshot> WaitForEpicReplyAsync(string anchorMessageId, string command)
        {
            var waitedMs = 0;
            while (waitedMs < ReplyTimeoutMs)
            {
                await Task.Delay(ReplyPollDelayMs);
                waitedMs += ReplyPollDelayMs;

                if (await _chatClient.HasEpicReplyForCommandAfterMessageAsync(anchorMessageId, command))
                {
                    return new DiscordMessageSnapshot(string.Empty, string.Empty, "EPIC RPG");
                }
            }

            return null;
        }
    }
}
