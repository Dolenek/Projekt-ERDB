using EpicRPGBot.UI.CardHand;
using Xunit;
using static EpicRPGBot.Tests.CardHand.CardHandTestCards;

namespace EpicRPGBot.Tests.CardHand;

public sealed class SampledExpectimaxDecisionEngineTests
{
    [Fact]
    public async Task FourCardRoyalDraw_PrefersPass()
    {
        var state = new CardHandState(Cards("H10", "HJ", "HQ", "HK"));
        var solver = new SampledExpectimaxDecisionEngine(TimeSpan.FromMilliseconds(20), 2);

        var result = await solver.DecideAsync(
            state,
            Array.Empty<CardId>(),
            CardHandRewardWeights.Default,
            CancellationToken.None);

        Assert.Equal(CardHandActionKind.Pass, result.Action.Kind);
        Assert.True(result.SampleCount >= state.GetUnseenCards().Count);
    }

    [Fact]
    public async Task Decision_IsCancelledBeforeSearch()
    {
        var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var solver = new SampledExpectimaxDecisionEngine(TimeSpan.FromMilliseconds(20), 1);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => solver.DecideAsync(
            new CardHandState(Cards("H2", "D3")),
            Array.Empty<CardId>(),
            CardHandRewardWeights.Default,
            cancellation.Token));
    }

    [Fact]
    public async Task SampledDecision_IsDeterministicForTheSameState()
    {
        var state = new CardHandState(Cards("H2", "D3"));
        var firstSolver = new SampledExpectimaxDecisionEngine(TimeSpan.Zero, 2);
        var secondSolver = new SampledExpectimaxDecisionEngine(TimeSpan.Zero, 2);

        var first = await firstSolver.DecideAsync(
            state, Array.Empty<CardId>(), CardHandRewardWeights.Default, CancellationToken.None);
        var second = await secondSolver.DecideAsync(
            state, Array.Empty<CardId>(), CardHandRewardWeights.Default, CancellationToken.None);

        Assert.Equal(first.Action.ToString(), second.Action.ToString());
        Assert.Equal(first.ExpectedUtility, second.ExpectedUtility);
        Assert.Equal(first.SampleCount, second.SampleCount);
    }

    [Fact]
    public void EffectiveUtilityTie_PrefersPass()
    {
        var estimate = new CardHandActionEstimate(10m, 0.5d, 1);

        var isBetter = CardHandEstimateComparer.IsBetter(
            estimate,
            CardHandAction.Pass,
            estimate,
            CardHandAction.Discard(Card("H2")));

        Assert.True(isBetter);
    }
}
