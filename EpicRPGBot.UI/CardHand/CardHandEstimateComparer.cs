namespace EpicRPGBot.UI.CardHand
{
    internal static class CardHandEstimateComparer
    {
        private const decimal UtilityTieTolerance = 0.0001m;

        public static bool IsBetter(CardHandActionStatistics candidate, CardHandActionStatistics current)
        {
            var estimate = new CardHandActionEstimate(candidate.MeanUtility, candidate.MeanPositiveProbability, 0);
            var currentEstimate = new CardHandActionEstimate(current.MeanUtility, current.MeanPositiveProbability, 0);
            return IsBetter(estimate, candidate.Action, currentEstimate, current.Action);
        }

        public static bool IsBetter(
            CardHandActionEstimate candidate,
            CardHandAction candidateAction,
            CardHandActionEstimate current,
            CardHandAction currentAction)
        {
            if (candidate.Utility > current.Utility + UtilityTieTolerance) return true;
            if (current.Utility > candidate.Utility + UtilityTieTolerance) return false;
            if (candidate.PositiveProbability > current.PositiveProbability + 0.000001d) return true;
            if (current.PositiveProbability > candidate.PositiveProbability + 0.000001d) return false;
            if (candidateAction.Kind != currentAction.Kind) return candidateAction.Kind == CardHandActionKind.Pass;
            return candidateAction.DiscardedCard.CompareTo(currentAction.DiscardedCard) < 0;
        }
    }
}
