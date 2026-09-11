using System.Collections.Generic;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardDeckParseResult
    {
        public CardDeckParseResult(bool success, IReadOnlyCollection<CardId> ownedCards, string error)
        {
            Success = success;
            OwnedCards = ownedCards ?? new CardId[0];
            Error = error ?? string.Empty;
        }

        public bool Success { get; }
        public IReadOnlyCollection<CardId> OwnedCards { get; }
        public string Error { get; }
    }
}
