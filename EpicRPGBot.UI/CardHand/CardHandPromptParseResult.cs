using System.Collections.Generic;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandPromptParseResult
    {
        public CardHandPromptParseResult(
            bool isPrompt,
            bool isValid,
            IReadOnlyList<CardId> cards,
            IReadOnlyDictionary<CardId, DiscordMessageButton> discardButtons,
            DiscordMessageButton passButton,
            string error)
        {
            IsPrompt = isPrompt;
            IsValid = isValid;
            Cards = cards ?? new CardId[0];
            DiscardButtons = discardButtons ?? new Dictionary<CardId, DiscordMessageButton>();
            PassButton = passButton;
            Error = error ?? string.Empty;
        }

        public bool IsPrompt { get; }
        public bool IsValid { get; }
        public IReadOnlyList<CardId> Cards { get; }
        public IReadOnlyDictionary<CardId, DiscordMessageButton> DiscardButtons { get; }
        public DiscordMessageButton PassButton { get; }
        public string Error { get; }
    }
}
