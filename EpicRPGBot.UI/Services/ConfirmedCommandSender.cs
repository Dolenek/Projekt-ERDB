using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class ConfirmedCommandSender
    {
        private const int PostOutgoingRegistrationDelayMs = 500;
        private const int ReplyTimeoutMs = 10000;
        private const int ReplyPollDelayMs = 250;
        private const int RetryDelayMs = 1000;
        private const int MaxAttempts = 3;
        private readonly IDiscordChatClient _chatClient;
        private readonly Func<int, CancellationToken, Task> _delay;

        public ConfirmedCommandSender(IDiscordChatClient chatClient) : this(chatClient, Task.Delay) { }

        internal ConfirmedCommandSender(IDiscordChatClient chatClient, Func<int, CancellationToken, Task> delay)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        }

        public async Task<ConfirmedCommandSendResult> SendAsync(
            string command,
            Action<DiscordMessageSnapshot> onOutgoingRegistered = null,
            CancellationToken cancellationToken = default)
        {
            var allowedAttempts = DiscordCommandSendPolicy.AllowsBlindResend(command) ? MaxAttempts : 1;
            ConfirmedCommandSendResult result = null;
            for (var attempt = 1; attempt <= allowedAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = await SendAttemptAsync(command, attempt, onOutgoingRegistered, cancellationToken);
                if (result.IsConfirmed) return result;
                if (attempt < allowedAttempts) await _delay(RetryDelayMs, cancellationToken);
            }
            return result;
        }

        public static bool RequiresReplyConfirmation(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                message.TrimStart().StartsWith("rpg ", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<ConfirmedCommandSendResult> SendAttemptAsync(string command, int attempt,
            Action<DiscordMessageSnapshot> onOutgoingRegistered, CancellationToken cancellationToken)
        {
            var outgoing = await _chatClient.SendMessageAndWaitForOutgoingAsync(command, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(outgoing?.Id)) return new ConfirmedCommandSendResult(null, null, attempt);
            onOutgoingRegistered?.Invoke(outgoing);
            await _delay(PostOutgoingRegistrationDelayMs, cancellationToken);
            var reply = await WaitForEpicReplyAsync(outgoing.Id, cancellationToken);
            return new ConfirmedCommandSendResult(outgoing, reply, attempt);
        }

        private async Task<DiscordMessageSnapshot> WaitForEpicReplyAsync(
            string outgoingMessageId, CancellationToken cancellationToken)
        {
            for (var waitedMs = 0; waitedMs < ReplyTimeoutMs; waitedMs += ReplyPollDelayMs)
            {
                await _delay(ReplyPollDelayMs, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var reply = await _chatClient.GetEpicReplyAfterMessageAsync(outgoingMessageId);
                cancellationToken.ThrowIfCancellationRequested();
                if (DiscordCommandReplySelector.IsReplyFor(reply, outgoingMessageId)) return reply;
                var recentMessages = await _chatClient.GetRecentMessagesAsync(20);
                cancellationToken.ThrowIfCancellationRequested();
                reply = DiscordCommandReplySelector.SelectFirst(recentMessages, outgoingMessageId);
                if (reply != null) return reply;
            }
            return null;
        }
    }
}
