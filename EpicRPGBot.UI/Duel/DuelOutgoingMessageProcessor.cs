using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    internal sealed class DuelOutgoingMessageProcessor
    {
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelMessageParser _messageParser;
        private readonly DuelWeaponSelector _weaponSelector;

        public DuelOutgoingMessageProcessor(
            IDuelDiscordClient chatClient,
            DuelMessageParser messageParser,
            DuelWeaponSelector weaponSelector)
        {
            _chatClient = chatClient;
            _messageParser = messageParser;
            _weaponSelector = weaponSelector;
        }

        public async Task<DuelAttemptOutcome?> ProcessAsync(
            IReadOnlyList<DiscordMessageSnapshot> messages,
            string playerName,
            ISet<string> processedSignatures,
            ISet<string> selectedWeaponPrompts,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            foreach (var message in messages)
            {
                var classification = _messageParser.Parse(message);
                if (!processedSignatures.Add(DuelMessageSequence.Signature(classification)))
                {
                    continue;
                }

                var outcome = await ProcessOneAsync(
                    classification,
                    playerName,
                    selectedWeaponPrompts,
                    report,
                    cancellationToken);
                if (outcome.HasValue)
                {
                    return outcome;
                }
            }

            return null;
        }

        private async Task<DuelAttemptOutcome?> ProcessOneAsync(
            DuelMessageClassification classification,
            string playerName,
            ISet<string> selectedWeaponPrompts,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            switch (classification.Kind)
            {
                case DuelMessageKind.InitiatorConfirmation:
                    return await HandleConfirmationAsync(classification, playerName, cancellationToken);
                case DuelMessageKind.Busy:
                    return DuelAttemptOutcome.Busy;
                case DuelMessageKind.Cancelled:
                    return BelongsToPlayer(classification, playerName) ? DuelAttemptOutcome.Cancelled : null;
                case DuelMessageKind.Result:
                    return BelongsToPlayer(classification, playerName) ? DuelAttemptOutcome.Completed : null;
                case DuelMessageKind.WeaponChoice:
                    return await HandleWeaponAsync(
                        classification,
                        playerName,
                        selectedWeaponPrompts,
                        report,
                        cancellationToken);
                default:
                    return null;
            }
        }

        private async Task<DuelAttemptOutcome?> HandleConfirmationAsync(
            DuelMessageClassification classification,
            string playerName,
            CancellationToken cancellationToken)
        {
            if (!BelongsToPlayer(classification, playerName))
            {
                return null;
            }

            var button = DuelButtonSelector.Find(classification.Message, "yes");
            var clicked = button != null && await _chatClient.ClickMessageButtonByLabelAsync(
                classification.Message.Id,
                button.Label,
                cancellationToken);
            return clicked ? (DuelAttemptOutcome?)null : DuelAttemptOutcome.Uncertain;
        }

        private async Task<DuelAttemptOutcome?> HandleWeaponAsync(
            DuelMessageClassification classification,
            string playerName,
            ISet<string> selectedPrompts,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var prompt = classification.Message;
            if (!BelongsToPlayer(classification, playerName) || selectedPrompts.Contains(prompt.Id))
            {
                return null;
            }

            var weapon = _weaponSelector.Select(prompt);
            if (weapon == null || !await _chatClient.ClickMessageButtonByLabelAsync(
                    prompt.Id,
                    weapon.Label,
                    cancellationToken))
            {
                return DuelAttemptOutcome.Uncertain;
            }

            selectedPrompts.Add(prompt.Id);
            report?.Invoke($"Selected duel weapon {weapon.Label}.");
            return null;
        }

        private bool BelongsToPlayer(DuelMessageClassification classification, string playerName)
        {
            return _messageParser.BelongsToPlayer(classification, playerName);
        }
    }
}
