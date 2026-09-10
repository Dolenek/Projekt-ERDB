using System;

namespace PuzzleReplay
{
    internal sealed class WilsonIntervalEstimator : IBinomialIntervalEstimator
    {
        private const double CriticalValue = 1.959963984540054;

        public BinomialInterval Estimate(int successes, int trials)
        {
            if (trials <= 0 || successes < 0 || successes > trials)
                throw new ArgumentOutOfRangeException(nameof(trials), "Require 0 <= successes <= trials and trials > 0.");
            var proportion = (double)successes / trials;
            var squaredCriticalValue = CriticalValue * CriticalValue;
            var denominator = 1 + squaredCriticalValue / trials;
            var center = (proportion + squaredCriticalValue / (2 * trials)) / denominator;
            var radius = CriticalValue * Math.Sqrt(proportion * (1 - proportion) / trials +
                squaredCriticalValue / (4.0 * trials * trials)) / denominator;
            return new BinomialInterval
            {
                Successes = successes, Trials = trials, Estimate = proportion,
                Lower = Math.Max(0, center - radius), Upper = Math.Min(1, center + radius)
            };
        }
    }
}
