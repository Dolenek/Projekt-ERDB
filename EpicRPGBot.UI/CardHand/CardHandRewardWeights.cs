using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandRewardWeights
    {
        private readonly IReadOnlyDictionary<CardRewardKind, decimal> _values;

        public CardHandRewardWeights(IReadOnlyDictionary<CardRewardKind, decimal> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var normalized = new Dictionary<CardRewardKind, decimal>();
            foreach (CardRewardKind kind in Enum.GetValues(typeof(CardRewardKind)))
            {
                var value = values.TryGetValue(kind, out var configured) ? configured : 0m;
                if (value < 0m) throw new ArgumentOutOfRangeException(nameof(values));
                normalized[kind] = value;
            }

            _values = normalized;
        }

        public static CardHandRewardWeights Default => new CardHandRewardWeights(
            new Dictionary<CardRewardKind, decimal>
            {
                [CardRewardKind.TimeCapsule] = 100m,
                [CardRewardKind.RoundCard] = 50m,
                [CardRewardKind.EternalLootbox] = 50m,
                [CardRewardKind.GodlyLootbox] = 20m,
                [CardRewardKind.OmegaLootbox] = 2m,
                [CardRewardKind.Flask] = 2m,
                [CardRewardKind.TimeCookie] = 1m,
                [CardRewardKind.GuildRing] = 0m,
                [CardRewardKind.ArenaCookie] = 0m
            });

        public decimal Get(CardRewardKind kind) => _values[kind];

        public IReadOnlyDictionary<CardRewardKind, decimal> ToDictionary()
        {
            var copy = new Dictionary<CardRewardKind, decimal>();
            foreach (var pair in _values) copy[pair.Key] = pair.Value;
            return copy;
        }
    }
}
