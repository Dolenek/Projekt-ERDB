using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandClassification
    {
        public CardHandClassification(CardHandKind kind, decimal rankBonus)
        {
            Kind = kind;
            RankBonus = rankBonus;
        }

        public CardHandKind Kind { get; }
        public decimal RankBonus { get; }
    }

    public sealed class CardHandClassifier
    {
        public CardHandClassification Classify(IReadOnlyList<CardId> cards)
        {
            if (cards == null || cards.Count != 5 || cards.Distinct().Count() != 5)
            {
                throw new ArgumentException("A final hand must contain five unique cards.", nameof(cards));
            }

            var groups = cards.Where(card => !card.IsJoker)
                .GroupBy(card => card.Rank)
                .OrderByDescending(group => group.Count())
                .ThenByDescending(group => group.Key)
                .ToArray();
            var ranks = cards.Where(card => !card.IsJoker).Select(card => card.Rank).OrderBy(rank => rank).ToArray();
            var hasJoker = cards.Any(card => card.IsJoker);
            var sameSuit = !hasJoker && cards.Select(card => card.Suit).Distinct().Count() == 1;
            var royal = IsRoyal(ranks);
            var aceLow = IsAceLow(ranks);
            var sequential = IsSequential(ranks);

            if (hasJoker && Count(groups, CardRank.Ace) == 4) return Result(CardHandKind.AceExtravaganza, CardRank.Ace);
            if (royal && sameSuit && cards[0].Suit == CardSuit.Hearts) return Result(CardHandKind.RoyalHeartedFlush);
            if (royal && sameSuit) return Result(CardHandKind.RoyalFlush);
            if (Count(groups, CardRank.Ace) == 4) return Result(CardHandKind.AceGala, CardRank.Ace);
            if (sequential && sameSuit && !royal) return Result(CardHandKind.StraightFlush);

            var four = groups.FirstOrDefault(group => group.Count() == 4);
            if (four != null) return Result(CardHandKind.FourOfAKind, four.Key);
            if (IsFullHouse(groups)) return ClassifyFullHouse(groups);
            if (IsGameOfKings(groups)) return Result(CardHandKind.GameOfKings, Average(CardRank.King, CardRank.Queen));
            if (sameSuit) return Result(CardHandKind.Flush);
            if (aceLow && HasAtLeastTwoSuits(cards)) return Result(CardHandKind.UnbreakableFortress);
            if (sequential && !aceLow && HasAtLeastTwoSuits(cards)) return Result(CardHandKind.Straight);

            var triple = groups.FirstOrDefault(group => group.Count() == 3);
            if (triple != null) return Result(CardHandKind.ThreeOfAKind, triple.Key);
            var pairs = groups.Where(group => group.Count() == 2).ToArray();
            if (pairs.Length == 2) return Result(CardHandKind.TwoPairs, Average(pairs[0].Key, pairs[1].Key));
            if (pairs.Length == 1) return Result(CardHandKind.Pair, pairs[0].Key);
            return Result(CardHandKind.RandomCards);
        }

        private static CardHandClassification ClassifyFullHouse(IReadOnlyList<IGrouping<CardRank, CardId>> groups)
        {
            var triple = groups.Single(group => group.Count() == 3).Key;
            var pair = groups.Single(group => group.Count() == 2).Key;
            return Result(CardHandKind.FullHouse, ((2m * RankBonus(triple)) + RankBonus(pair)) / 3m);
        }

        private static bool IsFullHouse(IReadOnlyList<IGrouping<CardRank, CardId>> groups)
        {
            return groups.Count == 2 && groups.Any(group => group.Count() == 3) && groups.Any(group => group.Count() == 2);
        }

        private static bool IsGameOfKings(IEnumerable<IGrouping<CardRank, CardId>> groups)
        {
            return groups.Any(group => group.Key == CardRank.King && group.Count() == 2) &&
                   groups.Any(group => group.Key == CardRank.Queen && group.Count() == 2);
        }

        private static int Count(IEnumerable<IGrouping<CardRank, CardId>> groups, CardRank rank)
        {
            return groups.FirstOrDefault(group => group.Key == rank)?.Count() ?? 0;
        }

        private static bool IsRoyal(IReadOnlyList<CardRank> ranks)
        {
            return ranks.SequenceEqual(new[] { CardRank.Ten, CardRank.Jack, CardRank.Queen, CardRank.King, CardRank.Ace });
        }

        private static bool IsAceLow(IReadOnlyList<CardRank> ranks)
        {
            return ranks.SequenceEqual(new[] { CardRank.Two, CardRank.Three, CardRank.Four, CardRank.Five, CardRank.Ace });
        }

        private static bool IsSequential(IReadOnlyList<CardRank> ranks)
        {
            if (ranks.Count != 5 || ranks.Distinct().Count() != 5) return false;
            if (IsAceLow(ranks)) return true;
            return Enumerable.Range(1, 4).All(index => (int)ranks[index] == (int)ranks[0] + index);
        }

        private static bool HasAtLeastTwoSuits(IEnumerable<CardId> cards)
        {
            return cards.Where(card => !card.IsJoker).Select(card => card.Suit).Distinct().Count() >= 2;
        }

        private static decimal Average(CardRank first, CardRank second)
        {
            return (RankBonus(first) + RankBonus(second)) / 2m;
        }

        private static CardHandClassification Result(CardHandKind kind, CardRank rank)
        {
            return Result(kind, RankBonus(rank));
        }

        private static CardHandClassification Result(CardHandKind kind, decimal bonus = 0m)
        {
            return new CardHandClassification(kind, bonus);
        }

        private static decimal RankBonus(CardRank rank)
        {
            if (rank == CardRank.Ace) return 0.70m;
            if (rank == CardRank.King) return 0.65m;
            if (rank == CardRank.Queen) return 0.60m;
            if (rank == CardRank.Jack) return 0.55m;
            var numericRank = (int)rank;
            return numericRank >= 2 && numericRank <= 10 ? numericRank * 0.05m : 0m;
        }
    }
}
