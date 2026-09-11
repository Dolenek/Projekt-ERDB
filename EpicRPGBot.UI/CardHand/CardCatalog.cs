using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.CardHand
{
    public static class CardCatalog
    {
        private static readonly CardId[] Cards = BuildCards();

        public static IReadOnlyList<CardId> All => Cards;

        private static CardId[] BuildCards()
        {
            var cards = new List<CardId>();
            var suits = new[] { CardSuit.Hearts, CardSuit.Diamonds, CardSuit.Clubs, CardSuit.Spades };
            foreach (var suit in suits)
            {
                cards.AddRange(Enumerable.Range(2, 13).Select(rank => new CardId(suit, (CardRank)rank)));
            }

            cards.Add(CardId.Joker);
            return cards.ToArray();
        }
    }
}
