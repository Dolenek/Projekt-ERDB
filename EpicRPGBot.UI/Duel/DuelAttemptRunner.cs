using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelAttemptRunner
    {
        private const int PollDelayMs = 500;
        private const int RecentMessageCount = 50;
        private static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(70);
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelOutgoingMessageProcessor _messageProcessor;
        private readonly Func<DateTimeOffset> _utcNow;

        public DuelAttemptRunner(
            IDuelDiscordClient chatClient,
            DuelMessageParser messageParser,
            DuelWeaponSelector weaponSelector,
            Func<DateTimeOffset> utcNow = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _messageProcessor = new DuelOutgoingMessageProcessor(
                chatClient,
                messageParser ?? throw new ArgumentNullException(nameof(messageParser)),
                weaponSelector ?? throw new ArgumentNullException(nameof(weaponSelector)));
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task<DuelAttemptOutcome> RunOutgoingAsync(
            DuelOffer offer,
            string playerName,
            Action<string> report,
            Action challengeMayBeActive,
            CancellationToken cancellationToken)
        {
            if (!await _chatClient.NavigateToChannelAndWaitAsync(
                DuelChannelCatalog.OutgoingDuelChannelUrl,
                cancellationToken))
            {
                report?.Invoke("Outgoing duel channel did not load.");
                return DuelAttemptOutcome.FailedSafe;
            }

            var command = "rpg duel " + offer.Message.AuthorId;
            challengeMayBeActive?.Invoke();
            var outgoing = await _chatClient.SendMessageAndWaitForOutgoingAsync(command, cancellationToken);
            if (outgoing == null)
            {
                report?.Invoke("Duel command registration is uncertain; it will not be resent.");
                return DuelAttemptOutcome.Uncertain;
            }

            report?.Invoke($"Sent one duel request to Discord user {offer.Message.AuthorId}.");
            return await ObserveOutgoingAttemptAsync(outgoing.Id, playerName, report, cancellationToken);
        }

        private async Task<DuelAttemptOutcome> ObserveOutgoingAttemptAsync(
            string outgoingMessageId,
            string playerName,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var processedSignatures = new HashSet<string>(StringComparer.Ordinal);
            var selectedWeaponPrompts = new HashSet<string>(StringComparer.Ordinal);
            var deadline = _utcNow() + AttemptTimeout;
            while (_utcNow() < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var messages = await _chatClient.GetRecentMessagesAsync(RecentMessageCount);
                var outcome = await _messageProcessor.ProcessAsync(
                    DuelMessageSequence.After(messages, outgoingMessageId),
                    playerName,
                    processedSignatures,
                    selectedWeaponPrompts,
                    report,
                    cancellationToken);
                if (outcome.HasValue)
                {
                    return outcome.Value;
                }

                await Task.Delay(PollDelayMs, cancellationToken);
            }

            report?.Invoke("Opponent did not accept the duel within 70 seconds.");
            return DuelAttemptOutcome.Cancelled;
        }
    }
}
