using System;
using System.Collections.Generic;
using System.Threading;

namespace EpicRPGBot.UI.CardHand
{
    internal sealed class CardHandExpectimaxEstimator
    {
        private readonly int _branchSamples;
        private readonly CardHandRewardEvaluator _rewardEvaluator;

        public CardHandExpectimaxEstimator(int branchSamples, CardHandRewardEvaluator rewardEvaluator)
        {
            _branchSamples = Math.Max(1, branchSamples);
            _rewardEvaluator = rewardEvaluator ?? new CardHandRewardEvaluator();
        }

        public CardHandActionEstimate EstimateAction(
            CardHandState state,
            CardHandAction action,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            Random random,
            CancellationToken token,
            bool enumerateChance,
            IDictionary<string, CardHandActionEstimate> memoizedStates)
        {
            return enumerateChance
                ? EnumerateFinalOutcomes(state, action, ownedCards, weights, token)
                : SampleOutcomes(state, action, ownedCards, weights, random, token, memoizedStates);
        }

        private CardHandActionEstimate SampleOutcomes(
            CardHandState state,
            CardHandAction action,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            Random random,
            CancellationToken token,
            IDictionary<string, CardHandActionEstimate> memoizedStates)
        {
            decimal utility = 0m;
            double positive = 0d;
            var terminalSamples = 0;
            var outcomes = DrawRandomOutcomes(
                state.GetUnseenCards(),
                RequiredDraws(action),
                _branchSamples,
                random);
            foreach (var outcome in outcomes)
            {
                token.ThrowIfCancellationRequested();
                var next = state.Apply(action, outcome);
                var estimate = next.IsComplete
                    ? EvaluateTerminal(next, ownedCards, weights)
                    : EstimateBestDecision(next, ownedCards, weights, random, token, memoizedStates);
                utility += estimate.Utility;
                positive += estimate.PositiveProbability;
                terminalSamples += estimate.TerminalSamples;
            }

            return new CardHandActionEstimate(utility / outcomes.Count, positive / outcomes.Count, terminalSamples);
        }

        private CardHandActionEstimate EstimateBestDecision(
            CardHandState state,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            Random random,
            CancellationToken token,
            IDictionary<string, CardHandActionEstimate> memoizedStates)
        {
            var key = BuildCanonicalKey(state);
            if (memoizedStates != null && memoizedStates.TryGetValue(key, out var cached)) return cached;
            CardHandActionEstimate? best = null;
            CardHandAction bestAction = null;
            foreach (var action in state.GetLegalActions())
            {
                var estimate = SampleOutcomes(state, action, ownedCards, weights, random, token, memoizedStates);
                if (!best.HasValue || CardHandEstimateComparer.IsBetter(estimate, action, best.Value, bestAction))
                {
                    best = estimate;
                    bestAction = action;
                }
            }

            var result = best ?? new CardHandActionEstimate(0m, 0d, 0);
            if (memoizedStates != null) memoizedStates[key] = result;
            return result;
        }

        private CardHandActionEstimate EnumerateFinalOutcomes(
            CardHandState state,
            CardHandAction action,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            CancellationToken token)
        {
            var unseen = state.GetUnseenCards();
            decimal utility = 0m;
            var positive = 0;
            var count = 0;
            foreach (var draws in EnumerateDraws(unseen, RequiredDraws(action)))
            {
                token.ThrowIfCancellationRequested();
                var estimate = EvaluateTerminal(state.Apply(action, draws), ownedCards, weights);
                utility += estimate.Utility;
                if (estimate.PositiveProbability > 0d) positive++;
                count++;
            }

            return new CardHandActionEstimate(utility / count, (double)positive / count, count);
        }

        private CardHandActionEstimate EvaluateTerminal(
            CardHandState state,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights)
        {
            var utility = _rewardEvaluator.Evaluate(state.Hand, ownedCards, weights).Utility;
            return new CardHandActionEstimate(utility, utility > 0m ? 1d : 0d, 1);
        }

        private static IEnumerable<IReadOnlyList<CardId>> EnumerateDraws(
            IReadOnlyList<CardId> cards,
            int drawCount)
        {
            if (drawCount == 1)
            {
                foreach (var card in cards) yield return new[] { card };
                yield break;
            }

            for (var first = 0; first < cards.Count - 1; first++)
            for (var second = first + 1; second < cards.Count; second++)
                yield return new[] { cards[first], cards[second] };
        }

        private static IReadOnlyList<IReadOnlyList<CardId>> DrawRandomOutcomes(
            IReadOnlyList<CardId> cards,
            int drawCount,
            int requestedCount,
            Random random)
        {
            var maximumCount = drawCount == 1 ? cards.Count : (cards.Count * (cards.Count - 1)) / 2;
            var targetCount = Math.Min(requestedCount, maximumCount);
            var selected = new HashSet<int>();
            var outcomes = new List<IReadOnlyList<CardId>>(targetCount);
            while (outcomes.Count < targetCount)
            {
                var first = random.Next(cards.Count);
                if (drawCount == 1)
                {
                    if (selected.Add(first)) outcomes.Add(new[] { cards[first] });
                    continue;
                }

                var second = random.Next(cards.Count - 1);
                if (second >= first) second++;
                var low = Math.Min(first, second);
                var high = Math.Max(first, second);
                if (selected.Add((low * cards.Count) + high)) outcomes.Add(new[] { cards[low], cards[high] });
            }

            return outcomes;
        }

        private static string BuildCanonicalKey(CardHandState state)
        {
            return string.Join(",", state.Hand) + "|" + string.Join(",", state.Discarded);
        }

        private static int RequiredDraws(CardHandAction action)
        {
            return action.Kind == CardHandActionKind.Pass ? 1 : 2;
        }
    }
}
