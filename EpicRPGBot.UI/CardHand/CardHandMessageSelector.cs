using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandMessageSelector
    {
        private readonly CardHandPromptParser _promptParser;

        public CardHandMessageSelector(CardHandPromptParser promptParser = null)
        {
            _promptParser = promptParser ?? new CardHandPromptParser();
        }

        public IReadOnlyList<DiscordMessageSnapshot> FindCandidates(
            IReadOnlyList<DiscordMessageSnapshot> messages,
            DiscordMessageSnapshot current,
            int expectedCardCount)
        {
            if (messages == null || current == null) return Array.Empty<DiscordMessageSnapshot>();
            var currentSignature = BuildSignature(current);
            return messages
                .Where(message => IsCorrelated(message, current, currentSignature))
                .Where(message => IsExpectedPromptOrResult(message, expectedCardCount))
                .OrderByDescending(message => IsNewer(message.Id, current.Id))
                .ThenByDescending(message => ReadSnowflake(message.Id))
                .ToArray();
        }

        private bool IsExpectedPromptOrResult(DiscordMessageSnapshot message, int expectedCardCount)
        {
            if (_promptParser.IsResult(message)) return true;
            var prompt = _promptParser.Parse(message);
            return prompt.IsValid && prompt.Cards.Count == expectedCardCount;
        }

        private static bool IsCorrelated(
            DiscordMessageSnapshot message,
            DiscordMessageSnapshot current,
            string currentSignature)
        {
            if (message == null) return false;
            if (IsNewer(message.Id, current.Id)) return true;
            return message.Id == current.Id && BuildSignature(message) != currentSignature;
        }

        private static bool IsNewer(string candidateId, string currentId)
        {
            var candidate = ReadSnowflake(candidateId);
            var current = ReadSnowflake(currentId);
            return candidate.HasValue && current.HasValue && candidate.Value > current.Value;
        }

        private static ulong? ReadSnowflake(string messageId)
        {
            var segment = (messageId ?? string.Empty).Split('-').LastOrDefault() ?? string.Empty;
            return ulong.TryParse(segment, out var value) ? value : (ulong?)null;
        }

        private static string BuildSignature(DiscordMessageSnapshot message)
        {
            return (message?.Text ?? string.Empty) + "|" +
                   string.Join(",", message?.Buttons.Select(button => button.Label) ?? Enumerable.Empty<string>());
        }
    }
}
