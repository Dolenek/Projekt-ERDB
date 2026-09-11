using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelIncomingAttemptRunner
    {
        private const int PollDelayMs = 500;
        private const int RecentMessageCount = 50;
        private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(70);
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelMessageParser _messageParser;
        private readonly Func<DateTimeOffset> _utcNow;

        public DuelIncomingAttemptRunner(
            IDuelDiscordClient chatClient,
            DuelMessageParser messageParser,
            Func<DateTimeOffset> utcNow = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _messageParser = messageParser ?? throw new ArgumentNullException(nameof(messageParser));
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task<DuelAttemptOutcome> AcceptAndWaitAsync(
            DuelIncomingRequest request,
            string playerName,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var prompt = request.Classification.Message;
            var yes = DuelButtonSelector.Find(prompt, "yes");
            if (yes == null || !await _chatClient.ClickMessageButtonByLabelAsync(
                    prompt.Id,
                    yes.Label,
                    cancellationToken))
            {
                report?.Invoke("Incoming duel could not be accepted.");
                return DuelAttemptOutcome.Cancelled;
            }

            report?.Invoke("Accepted incoming duel and will feed without choosing a weapon.");
            return await WaitForTerminalAsync(
                prompt.Id,
                playerName,
                request.Classification.InitiatorName,
                cancellationToken);
        }

        private async Task<DuelAttemptOutcome> WaitForTerminalAsync(
            string promptMessageId,
            string playerName,
            string initiatorName,
            CancellationToken cancellationToken)
        {
            var processed = new HashSet<string>(StringComparer.Ordinal);
            var deadline = _utcNow() + AttemptTimeout;
            while (_utcNow() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var messages = await _chatClient.GetRecentMessagesAsync(RecentMessageCount);
                foreach (var message in FromAnchor(messages, promptMessageId))
                {
                    var classification = _messageParser.Parse(message);
                    if (!processed.Add(DuelMessageSequence.Signature(classification)) ||
                        !BelongsToDuel(classification, playerName, initiatorName))
                    {
                        continue;
                    }

                    if (classification.Kind == DuelMessageKind.Result)
                    {
                        return DuelAttemptOutcome.Completed;
                    }

                    if (classification.Kind == DuelMessageKind.Cancelled)
                    {
                        return DuelAttemptOutcome.Cancelled;
                    }
                }

                await Task.Delay(PollDelayMs, cancellationToken);
            }

            return DuelAttemptOutcome.Uncertain;
        }

        private bool BelongsToDuel(
            DuelMessageClassification classification,
            string playerName,
            string initiatorName)
        {
            return _messageParser.BelongsToPlayer(classification, playerName) ||
                   _messageParser.BelongsToPlayer(classification, initiatorName);
        }

        private static IReadOnlyList<DiscordMessageSnapshot> FromAnchor(
            IReadOnlyList<DiscordMessageSnapshot> messages,
            string anchorMessageId)
        {
            var index = messages?.ToList().FindIndex(message => message?.Id == anchorMessageId) ?? -1;
            return messages?.Skip(Math.Max(0, index)).ToArray() ?? Array.Empty<DiscordMessageSnapshot>();
        }
    }
}
