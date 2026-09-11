using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandPromptParser
    {
        private static readonly string[] ResultNames =
        {
            "ace extravaganza", "royal hearted flush", "royal flush", "ace gala",
            "straight flush", "four of a kind", "full house", "game of kings", "flush",
            "unbreakable fortress", "straight", "three of a kind", "two pairs", "pair",
            "random cards", "timeout"
        };

        public CardHandPromptParseResult Parse(DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null) return Empty();
            var pass = snapshot.Buttons.FirstOrDefault(IsPassButton);
            var cardButtons = new Dictionary<CardId, DiscordMessageButton>();
            var invalidCardLabel = false;
            foreach (var button in snapshot.Buttons.Where(button => !IsUtilityButton(button)))
            {
                if (!CardId.TryParse(button.Label, out var card))
                {
                    invalidCardLabel = true;
                    continue;
                }

                if (cardButtons.ContainsKey(card))
                    return Invalid(true, cardButtons, pass, "Duplicate card buttons were found.");
                cardButtons[card] = button;
            }

            var isPrompt = pass != null || LooksLikeCardHandText(snapshot.Text) || cardButtons.Count >= 2;
            if (!isPrompt) return Empty();
            if (pass == null) return Invalid(true, cardButtons, null, "Pass button was not found.");
            if (invalidCardLabel) return Invalid(true, cardButtons, pass, "An unknown action button was found.");
            if (cardButtons.Count < 2 || cardButtons.Count > 4)
                return Invalid(true, cardButtons, pass, "Expected two to four card buttons.");

            return new CardHandPromptParseResult(
                true,
                true,
                cardButtons.Keys.OrderBy(card => card).ToArray(),
                cardButtons,
                pass,
                string.Empty);
        }

        public bool IsResult(DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null) return false;
            var message = (snapshot.Text ?? string.Empty).ToLowerInvariant();
            return (message.Contains("reward") && message.Contains("owned")) ||
                   ResultNames.Any(message.Contains);
        }

        private static bool IsPassButton(DiscordMessageButton button)
        {
            return string.Equals(button?.Label?.Trim(), "pass", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUtilityButton(DiscordMessageButton button)
        {
            var label = button?.Label?.Trim() ?? string.Empty;
            return IsPassButton(button) || string.Equals(label, "hands", StringComparison.OrdinalIgnoreCase);
        }

        private static bool LooksLikeCardHandText(string message)
        {
            var normalized = (message ?? string.Empty).ToLowerInvariant();
            return normalized.Contains("card hand") || normalized.Contains("select a card") ||
                   (normalized.Contains("pass") && normalized.Contains("cards"));
        }

        private static CardHandPromptParseResult Empty()
        {
            return new CardHandPromptParseResult(false, false, null, null, null, string.Empty);
        }

        private static CardHandPromptParseResult Invalid(
            bool isPrompt,
            IReadOnlyDictionary<CardId, DiscordMessageButton> buttons,
            DiscordMessageButton pass,
            string error)
        {
            return new CardHandPromptParseResult(
                isPrompt,
                false,
                buttons.Keys.OrderBy(card => card).ToArray(),
                buttons,
                pass,
                error);
        }
    }
}
