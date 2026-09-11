namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandDecisionResult
    {
        public CardHandDecisionResult(
            CardHandAction action,
            decimal expectedUtility,
            double positivePayoutProbability,
            int sampleCount)
        {
            Action = action;
            ExpectedUtility = expectedUtility;
            PositivePayoutProbability = positivePayoutProbability;
            SampleCount = sampleCount;
        }

        public CardHandAction Action { get; }
        public decimal ExpectedUtility { get; }
        public double PositivePayoutProbability { get; }
        public int SampleCount { get; }
    }
}
