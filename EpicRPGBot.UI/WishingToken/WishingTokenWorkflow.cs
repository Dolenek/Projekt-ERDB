using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.WishingToken
{
    public sealed class WishingTokenWorkflow
    {
        private const int BusyRetryDelayMs = 1000;
        private const int NextCycleDelayMs = 500;
        private const int ResultPollDelayMs = 250;
        private const int ResultTimeoutMs = 10000;
        private const int ScanCount = 20;

        private readonly IDiscordChatClient _chatClient;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly WishingTokenReplyParser _replyParser;

        public WishingTokenWorkflow(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender confirmedCommandSender)
            : this(chatClient, confirmedCommandSender, new WishingTokenReplyParser())
        {
        }

        public WishingTokenWorkflow(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender confirmedCommandSender,
            WishingTokenReplyParser replyParser)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _confirmedCommandSender = confirmedCommandSender ?? throw new ArgumentNullException(nameof(confirmedCommandSender));
            _replyParser = replyParser ?? throw new ArgumentNullException(nameof(replyParser));
        }

        public async Task RunAsync(Action<string> report, CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                report?.Invoke("Sending rpg use wishing token.");
                var sendResult = await _confirmedCommandSender.SendAsync("rpg use wishing token", cancellationToken: cancellationToken);
                if (sendResult.OutgoingMessage == null)
                {
                    report?.Invoke("Stopped: failed to register the outgoing wishing-token command.");
                    return;
                }

                if (IsPreviousCommandBusy(sendResult.ReplyMessage))
                {
                    report?.Invoke("Previous command still finishing. Waiting 1 second and retrying.");
                    await Task.Delay(BusyRetryDelayMs, cancellationToken);
                    continue;
                }

                var wishMenuMessage = await ResolveWishMenuAsync(sendResult, cancellationToken);
                if (wishMenuMessage == null)
                {
                    report?.Invoke("Stopped: missing wish menu after the outgoing command.");
                    return;
                }

                var menuReply = _replyParser.Parse(wishMenuMessage.Text);
                report?.Invoke(menuReply.Summary);
                var clicked = await _chatClient.ClickMessageButtonAsync(wishMenuMessage.Id, 1, 2, cancellationToken);
                if (!clicked)
                {
                    report?.Invoke("Stopped: failed to select the time cookie button.");
                    return;
                }

                report?.Invoke("Selected time cookie.");
                var resultReply = await WaitForWishResultAsync(wishMenuMessage.Id, cancellationToken);
                if (resultReply == null)
                {
                    report?.Invoke("Stopped: timed out waiting for the wish result reply.");
                    return;
                }

                var parsed = _replyParser.Parse(resultReply.Text);
                if (parsed.Kind != WishingTokenReplyKind.Success &&
                    parsed.Kind != WishingTokenReplyKind.Failure)
                {
                    report?.Invoke("Stopped: EPIC RPG reply after selection was not a recognized result.");
                    return;
                }

                report?.Invoke(parsed.Summary);
                await Task.Delay(NextCycleDelayMs, cancellationToken);
            }
        }

        private async Task<DiscordMessageSnapshot> ResolveWishMenuAsync(
            ConfirmedCommandSendResult sendResult,
            CancellationToken cancellationToken)
        {
            if (sendResult?.ReplyMessage != null &&
                _replyParser.Parse(sendResult.ReplyMessage.Text).Kind == WishingTokenReplyKind.WishMenu)
            {
                return sendResult.ReplyMessage;
            }

            return await WaitForWishMenuAsync(sendResult?.OutgoingMessage?.Id, cancellationToken);
        }

        private async Task<DiscordMessageSnapshot> WaitForWishMenuAsync(string outgoingMessageId, CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < ResultTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(ResultPollDelayMs, cancellationToken);
                waitedMs += ResultPollDelayMs;

                var snapshots = await _chatClient.GetRecentMessagesAsync(ScanCount);
                var reply = FindWishMenuAfterOutgoing(snapshots, outgoingMessageId);
                if (reply != null)
                {
                    return reply;
                }
            }

            return null;
        }

        private async Task<DiscordMessageSnapshot> WaitForWishResultAsync(string anchorMessageId, CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < ResultTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(ResultPollDelayMs, cancellationToken);
                waitedMs += ResultPollDelayMs;

                var snapshots = await _chatClient.GetRecentMessagesAsync(ScanCount);
                var editedAnchor = FindMessageById(snapshots, anchorMessageId);
                if (IsRecognizedWishResult(editedAnchor))
                {
                    return editedAnchor;
                }

                var reply = FindRecognizedWishResultAfter(snapshots, anchorMessageId);
                if (reply != null)
                {
                    return reply;
                }
            }

            return null;
        }

        private bool IsRecognizedWishResult(DiscordMessageSnapshot snapshot)
        {
            var kind = _replyParser.Parse(snapshot?.Text).Kind;
            return kind == WishingTokenReplyKind.Success || kind == WishingTokenReplyKind.Failure;
        }

        private static bool IsPreviousCommandBusy(DiscordMessageSnapshot snapshot)
        {
            return TrackedCommandResponseClassifier.IsPreviousCommandBusyReply(snapshot);
        }

        private DiscordMessageSnapshot FindWishMenuAfterOutgoing(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string outgoingMessageId)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return null;
            }

            var startIndex = 0;
            for (var i = 0; i < snapshots.Count; i++)
            {
                if (!string.Equals(snapshots[i]?.Id, outgoingMessageId, StringComparison.Ordinal))
                {
                    continue;
                }

                startIndex = i + 1;
                break;
            }

            for (var i = startIndex; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (_replyParser.Parse(snapshot?.Text).Kind == WishingTokenReplyKind.WishMenu)
                {
                    return snapshot;
                }
            }

            return null;
        }

        private DiscordMessageSnapshot FindRecognizedWishResultAfter(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string anchorMessageId)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return null;
            }

            var startIndex = 0;
            for (var i = 0; i < snapshots.Count; i++)
            {
                if (!string.Equals(snapshots[i]?.Id, anchorMessageId, StringComparison.Ordinal))
                {
                    continue;
                }

                startIndex = i + 1;
                break;
            }

            for (var i = startIndex; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (IsRecognizedWishResult(snapshot))
                {
                    return snapshot;
                }
            }

            return null;
        }

        private static DiscordMessageSnapshot FindMessageById(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string messageId)
        {
            if (snapshots == null || snapshots.Count == 0 || string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (string.Equals(snapshot?.Id, messageId, StringComparison.Ordinal))
                {
                    return snapshot;
                }
            }

            return null;
        }
    }
}
