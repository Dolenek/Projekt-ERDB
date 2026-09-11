using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandRewardEvaluator
    {
        private readonly CardHandClassifier _classifier;

        public CardHandRewardEvaluator(CardHandClassifier classifier = null)
        {
            _classifier = classifier ?? new CardHandClassifier();
        }

        public CardHandEvaluation Evaluate(
            IReadOnlyList<CardId> cards,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights)
        {
            if (ownedCards == null) throw new ArgumentNullException(nameof(ownedCards));
            if (weights == null) throw new ArgumentNullException(nameof(weights));
            var classification = _classifier.Classify(cards);
            var owned = new HashSet<CardId>(ownedCards);
            var ownedCount = 0;
            foreach (var card in cards) if (owned.Contains(card)) ownedCount++;

            var ownershipMultiplier = 0.15m + (0.17m * ownedCount);
            var totalMultiplier = ownershipMultiplier * (1m + classification.RankBonus);
            var rewards = CalculateRewards(classification.Kind, totalMultiplier);
            var utility = CalculateUtility(rewards, weights);
            return new CardHandEvaluation(
                classification.Kind,
                ownershipMultiplier,
                classification.RankBonus,
                rewards,
                utility);
        }

        private static IReadOnlyDictionary<CardRewardKind, int> CalculateRewards(CardHandKind kind, decimal multiplier)
        {
            var rewards = new Dictionary<CardRewardKind, int>();
            foreach (var baseReward in CardHandRewardCatalog.Get(kind))
            {
                rewards[baseReward.Key] = (int)Math.Floor(baseReward.Value * multiplier);
            }

            return rewards;
        }

        private static decimal CalculateUtility(
            IReadOnlyDictionary<CardRewardKind, int> rewards,
            CardHandRewardWeights weights)
        {
            var utility = 0m;
            foreach (var reward in rewards) utility += reward.Value * weights.Get(reward.Key);
            return utility;
        }
    }
}
