using EpicRPGBot.UI.CardHand;
using Xunit;
using static EpicRPGBot.Tests.CardHand.CardHandTestCards;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardHandRewardEvaluatorTests
{
    [Fact]
    public void TwoPairs_ReproducesObservedPerRewardFlooring()
    {
        var hand = Cards("H3", "D3", "C4", "S4", "HA");
        var owned = hand.Take(4).ToArray();
        var result = new CardHandRewardEvaluator().Evaluate(hand, owned, CardHandRewardWeights.Default);

        Assert.Equal(CardHandKind.TwoPairs, result.HandKind);
        Assert.Equal(0.83m, result.OwnershipMultiplier);
        Assert.Equal(24, result.Rewards[CardRewardKind.TimeCookie]);
        Assert.Equal(78, result.Rewards[CardRewardKind.GuildRing]);
        Assert.Equal(97, result.Rewards[CardRewardKind.ArenaCookie]);
    }

    [Theory]
    [InlineData(0, 0.15)]
    [InlineData(1, 0.32)]
    [InlineData(2, 0.49)]
    [InlineData(3, 0.66)]
    [InlineData(4, 0.83)]
    [InlineData(5, 1.00)]
    public void OwnershipMultiplier_UsesEveryOwnedCard(int ownedCount, double expectedMultiplier)
    {
        var hand = Cards("H2", "D4", "C6", "S8", "H10");
        var evaluator = new CardHandRewardEvaluator();

        var result = evaluator.Evaluate(hand, hand.Take(ownedCount).ToArray(), CardHandRewardWeights.Default);

        Assert.Equal((decimal)expectedMultiplier, result.OwnershipMultiplier);
    }

    [Fact]
    public void DefaultWeights_UseLatestRoundCardValue()
    {
        var weights = CardHandRewardWeights.Default;

        Assert.Equal(100m, weights.Get(CardRewardKind.TimeCapsule));
        Assert.Equal(50m, weights.Get(CardRewardKind.RoundCard));
        Assert.Equal(0m, weights.Get(CardRewardKind.GuildRing));
    }
}
