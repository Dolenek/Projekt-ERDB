using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandState
    {
        public CardHandState(IEnumerable<CardId> hand, IEnumerable<CardId> discarded = null)
        {
            Hand = Normalize(hand, nameof(hand));
            Discarded = Normalize(discarded ?? Enumerable.Empty<CardId>(), nameof(discarded));
            if (Hand.Count < 2 || Hand.Count > 5 || Hand.Intersect(Discarded).Any())
            {
                throw new ArgumentException("Card-hand state contains an invalid card set.");
            }
        }

        public IReadOnlyList<CardId> Hand { get; }
        public IReadOnlyList<CardId> Discarded { get; }
        public bool IsComplete => Hand.Count == 5;

        public IReadOnlyList<CardHandAction> GetLegalActions()
        {
            if (IsComplete) return new CardHandAction[0];
            var actions = new List<CardHandAction> { CardHandAction.Pass };
            actions.AddRange(Hand.Select(CardHandAction.Discard));
            return actions;
        }

        public IReadOnlyList<CardId> GetUnseenCards()
        {
            var seen = new HashSet<CardId>(Hand.Concat(Discarded));
            return CardCatalog.All.Where(card => !seen.Contains(card)).ToArray();
        }

        public CardHandState Apply(CardHandAction action, IEnumerable<CardId> drawnCards)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var draws = Normalize(drawnCards, nameof(drawnCards));
            var requiredDraws = action.Kind == CardHandActionKind.Pass ? 1 : 2;
            if (draws.Count != requiredDraws || draws.Any(card => !GetUnseenCards().Contains(card)))
            {
                throw new ArgumentException("The transition contains invalid draws.", nameof(drawnCards));
            }

            var hand = Hand.ToList();
            var discarded = Discarded.ToList();
            if (action.Kind == CardHandActionKind.Discard)
            {
                if (!hand.Remove(action.DiscardedCard)) throw new ArgumentException("Discard is not in hand.", nameof(action));
                discarded.Add(action.DiscardedCard);
            }

            hand.AddRange(draws);
            return new CardHandState(hand, discarded);
        }

        private static IReadOnlyList<CardId> Normalize(IEnumerable<CardId> cards, string argumentName)
        {
            if (cards == null) throw new ArgumentNullException(argumentName);
            var result = cards.OrderBy(card => card).ToArray();
            if (result.Distinct().Count() != result.Length)
            {
                throw new ArgumentException("Duplicate cards are not allowed.", argumentName);
            }

            return result;
        }
    }
}
