using System.Collections.Generic;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandEvaluation
    {
        public CardHandEvaluation(
            CardHandKind handKind,
            decimal ownershipMultiplier,
            decimal rankBonus,
            IReadOnlyDictionary<CardRewardKind, int> rewards,
            decimal utility)
        {
            HandKind = handKind;
            OwnershipMultiplier = ownershipMultiplier;
            RankBonus = rankBonus;
            Rewards = rewards;
            Utility = utility;
        }

        public CardHandKind HandKind { get; }
        public decimal OwnershipMultiplier { get; }
        public decimal RankBonus { get; }
        public IReadOnlyDictionary<CardRewardKind, int> Rewards { get; }
        public decimal Utility { get; }
    }
}
