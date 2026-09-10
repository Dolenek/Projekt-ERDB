using EpicRPGBot.UI.Puzzle;
using EpicRPGBot.UI.Puzzle.Local;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class PuzzleSolverServiceTests
{
    [Fact]
    public async Task ValidatedAnswer_SendsOnce_AndLeavesIncidentPaused()
    {
        var harness = new SolverHarness();
        await harness.Solve();
        Assert.Equal(new[] { "apple" }, harness.Sent);
        Assert.True(harness.Paused);
        Assert.True(harness.IncidentActive);
        Assert.False(harness.Solver.IsBusy);
    }

    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    public async Task UncertainUnvalidatedOrObservation_NeverSends(bool accepted, bool validated, bool enabled)
    {
        var harness = new SolverHarness(accepted, validated, enabled);
        await harness.Solve();
        Assert.Empty(harness.Sent);
        Assert.True(harness.Paused);
        Assert.True(harness.IncidentActive);
    }

    [Fact]
    public async Task GuardClearedDuringRecognition_CancelsAndDoesNotSend()
    {
        var harness = new SolverHarness();
        harness.Provider.WaitForRelease = true;
        var attempt = harness.Solve();
        await harness.Provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        harness.IncidentActive = false;
        harness.Solver.CancelCurrentSolve();
        harness.Provider.Release.TrySetResult(true);
        await attempt;
        Assert.Empty(harness.Sent);
        Assert.False(harness.Solver.IsBusy);
    }

    [Fact]
    public async Task ConcurrentDuplicate_UsesOneProviderCall()
    {
        var harness = new SolverHarness();
        harness.Provider.WaitForRelease = true;
        var attempt = harness.Solve();
        await harness.Provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await harness.Solve();
        harness.Provider.Release.TrySetResult(true);
        await attempt;
        Assert.Single(harness.Sent);
        Assert.Equal(1, harness.Provider.CallCount);
    }

    [Fact]
    public async Task MissingImage_LeavesManualResolutionAndDoesNotInitializeProvider()
    {
        var harness = new SolverHarness();
        harness.Images.Bytes = null;
        await harness.Solve();
        Assert.Empty(harness.Sent);
        Assert.Equal(0, harness.Provider.CallCount);
        Assert.True(harness.Paused);
    }

    [Fact]
    public async Task RecognitionFailure_LeavesManualResolution()
    {
        var harness = new SolverHarness();
        harness.Provider.Fail = true;
        await harness.Solve();
        Assert.Empty(harness.Sent);
        Assert.True(harness.Paused);
        Assert.False(harness.Solver.IsBusy);
    }
}
