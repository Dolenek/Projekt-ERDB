namespace EpicRPGBot.UI.CardHand
{
    internal readonly struct CardHandActionEstimate
    {
        public CardHandActionEstimate(decimal utility, double positiveProbability, int terminalSamples)
        {
            Utility = utility;
            PositiveProbability = positiveProbability;
            TerminalSamples = terminalSamples;
        }

        public decimal Utility { get; }
        public double PositiveProbability { get; }
        public int TerminalSamples { get; }
    }

    internal sealed class CardHandActionStatistics
    {
        private decimal _utilityTotal;
        private double _positiveTotal;

        public CardHandActionStatistics(CardHandAction action)
        {
            Action = action;
        }

        public CardHandAction Action { get; }
        public int Batches { get; private set; }
        public int TerminalSamples { get; private set; }
        public decimal MeanUtility => Batches == 0 ? 0m : _utilityTotal / Batches;
        public double MeanPositiveProbability => Batches == 0 ? 0d : _positiveTotal / Batches;

        public void Add(CardHandActionEstimate estimate)
        {
            _utilityTotal += estimate.Utility;
            _positiveTotal += estimate.PositiveProbability;
            TerminalSamples += estimate.TerminalSamples;
            Batches++;
        }
    }
}
