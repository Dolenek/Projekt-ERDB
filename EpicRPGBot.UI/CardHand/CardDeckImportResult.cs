using System.Collections.Generic;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardDeckImportResult
    {
        public CardDeckImportResult(bool success, IReadOnlyCollection<CardId> ownedCards, string message)
        {
            Success = success;
            OwnedCards = ownedCards ?? new CardId[0];
            Message = message ?? string.Empty;
        }

        public bool Success { get; }
        public IReadOnlyCollection<CardId> OwnedCards { get; }
        public string Message { get; }
    }
}
