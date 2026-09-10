using PuzzleReplay;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class WilsonIntervalTests
{
    private readonly IBinomialIntervalEstimator _estimator = new WilsonIntervalEstimator();

    [Theory]
    [InlineData(200, 200, 0.9811546736, 1)]
    [InlineData(0, 200, 0, 0.0188453264)]
    [InlineData(50, 100, 0.4038315304, 0.5961684696)]
    public void ReferenceIntervals_AreReproduced(int successes, int trials, double lower, double upper)
    {
        var interval = _estimator.Estimate(successes, trials);
        Assert.Equal(lower, interval.Lower, 9);
        Assert.Equal(upper, interval.Upper, 9);
        Assert.Equal((double)successes / trials, interval.Estimate);
    }

    [Fact]
    public void PerfectResults_Require3838TrialsForTargetLowerBound()
    {
        Assert.True(_estimator.Estimate(3837, 3837).Lower < 0.999);
        Assert.True(_estimator.Estimate(3838, 3838).Lower >= 0.999);
    }

    [Fact]
    public void CampaignSize_AllowsNoFailuresForStrictLowerBound()
    {
        Assert.True(_estimator.Estimate(3944, 3944).Lower >= 0.999);
        Assert.True(_estimator.Estimate(3943, 3944).Lower < 0.999);
        Assert.True(_estimator.Estimate(199, 200).Lower < 0.999);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 10)]
    [InlineData(11, 10)]
    public void InvalidCounts_AreRejected(int successes, int trials)
        => Assert.Throws<ArgumentOutOfRangeException>(() => _estimator.Estimate(successes, trials));
}
