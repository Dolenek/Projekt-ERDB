using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class SampledExpectimaxDecisionEngine : ICardHandDecisionEngine
    {
        private readonly TimeSpan _searchBudget;
        private readonly CardHandExpectimaxEstimator _estimator;

        public SampledExpectimaxDecisionEngine(
            TimeSpan? searchBudget = null,
            int branchSamples = 3,
            CardHandRewardEvaluator rewardEvaluator = null)
        {
            _searchBudget = searchBudget ?? TimeSpan.FromSeconds(5);
            _estimator = new CardHandExpectimaxEstimator(branchSamples, rewardEvaluator);
        }

        public Task<CardHandDecisionResult> DecideAsync(
            CardHandState state,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            CancellationToken cancellationToken)
        {
            return Task.Run(() => Decide(state, ownedCards, weights, cancellationToken), cancellationToken);
        }

        private CardHandDecisionResult Decide(
            CardHandState state,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            CancellationToken token)
        {
            ValidateInput(state, ownedCards, weights);
            var random = new Random(BuildSeed(state, ownedCards, weights));
            var statistics = state.GetLegalActions().Select(action => new CardHandActionStatistics(action)).ToArray();
            if (state.Hand.Count == 4) return DecideExactFinal(state, ownedCards, weights, statistics, token);

            var stopwatch = Stopwatch.StartNew();
            do
            {
                var memoizedStates = new Dictionary<string, CardHandActionEstimate>();
                foreach (var action in statistics)
                {
                    token.ThrowIfCancellationRequested();
                    action.Add(_estimator.EstimateAction(
                        state,
                        action.Action,
                        ownedCards,
                        weights,
                        random,
                        token,
                        false,
                        memoizedStates));
                    if (stopwatch.Elapsed >= _searchBudget && statistics.All(item => item.Batches > 0)) break;
                }
            }
            while (stopwatch.Elapsed < _searchBudget);

            return BuildResult(SelectBest(statistics));
        }

        private CardHandDecisionResult DecideExactFinal(
            CardHandState state,
            IReadOnlyCollection<CardId> ownedCards,
            CardHandRewardWeights weights,
            IEnumerable<CardHandActionStatistics> statistics,
            CancellationToken token)
        {
            var result = statistics.ToArray();
            foreach (var item in result)
            {
                token.ThrowIfCancellationRequested();
                item.Add(_estimator.EstimateAction(state, item.Action, ownedCards, weights, null, token, true, null));
            }

            return BuildResult(SelectBest(result));
        }

        private static CardHandActionStatistics SelectBest(IEnumerable<CardHandActionStatistics> candidates)
        {
            return candidates.Aggregate((best, candidate) => CardHandEstimateComparer.IsBetter(candidate, best) ? candidate : best);
        }

        private static CardHandDecisionResult BuildResult(CardHandActionStatistics best)
        {
            return new CardHandDecisionResult(best.Action, best.MeanUtility, best.MeanPositiveProbability, best.TerminalSamples);
        }

        private static void ValidateInput(CardHandState state, object ownedCards, object weights)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.IsComplete) throw new ArgumentException("A complete hand has no next action.", nameof(state));
            if (ownedCards == null) throw new ArgumentNullException(nameof(ownedCards));
            if (weights == null) throw new ArgumentNullException(nameof(weights));
        }

        private static int BuildSeed(
            CardHandState state,
            IEnumerable<CardId> ownedCards,
            CardHandRewardWeights weights)
        {
            unchecked
            {
                var seed = 17;
                foreach (var card in state.Hand.Concat(state.Discarded).Concat(ownedCards.OrderBy(card => card)))
                    seed = (seed * 31) + card.GetHashCode();
                foreach (CardRewardKind kind in Enum.GetValues(typeof(CardRewardKind)))
                    seed = (seed * 31) + weights.Get(kind).GetHashCode();
                return seed;
            }
        }
    }
}
