namespace PuzzleReplay
{
    internal interface IBinomialIntervalEstimator
    {
        BinomialInterval Estimate(int successes, int trials);
    }
}
