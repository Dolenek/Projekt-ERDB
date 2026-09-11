using EpicRPGBot.UI.CardHand;
using Xunit;
using static EpicRPGBot.Tests.CardHand.CardHandTestCards;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardHandClassifierTests
{
    public static IEnumerable<object[]> Hands()
    {
        yield return Case(CardHandKind.AceExtravaganza, "HA", "DA", "CA", "SA", "JOKER");
        yield return Case(CardHandKind.RoyalHeartedFlush, "H10", "HJ", "HQ", "HK", "HA");
        yield return Case(CardHandKind.RoyalFlush, "S10", "SJ", "SQ", "SK", "SA");
        yield return Case(CardHandKind.AceGala, "HA", "DA", "CA", "SA", "H2");
        yield return Case(CardHandKind.StraightFlush, "H2", "H3", "H4", "H5", "H6");
        yield return Case(CardHandKind.FourOfAKind, "H9", "D9", "C9", "S9", "HA");
        yield return Case(CardHandKind.FullHouse, "H7", "D7", "C7", "HQ", "DQ");
        yield return Case(CardHandKind.GameOfKings, "HK", "DK", "HQ", "DQ", "H2");
        yield return Case(CardHandKind.Flush, "H2", "H4", "H6", "H8", "H10");
        yield return Case(CardHandKind.UnbreakableFortress, "HA", "D2", "C3", "S4", "H5");
        yield return Case(CardHandKind.Straight, "H6", "D7", "C8", "S9", "H10");
        yield return Case(CardHandKind.ThreeOfAKind, "H7", "D7", "C7", "H2", "D3");
        yield return Case(CardHandKind.TwoPairs, "HQ", "DQ", "H7", "D7", "C2");
        yield return Case(CardHandKind.Pair, "H9", "D9", "C2", "S4", "H6");
        yield return Case(CardHandKind.RandomCards, "H2", "D4", "C6", "S8", "H10");
    }

    [Theory]
    [MemberData(nameof(Hands))]
    public void ClassifiesHandsInRewardPriority(CardHandKind expected, CardId[] cards)
    {
        Assert.Equal(expected, new CardHandClassifier().Classify(cards).Kind);
    }

    [Fact]
    public void FullHouse_UsesSuppliedWeightedRankBonus()
    {
        var result = new CardHandClassifier().Classify(Cards("H7", "D7", "C7", "HQ", "DQ"));

        Assert.Equal(0.4333333333333333333333333333m, result.RankBonus);
    }

    [Fact]
    public void Joker_IsNotWildForAFlush()
    {
        var result = new CardHandClassifier().Classify(Cards("H2", "H4", "H6", "H8", "JOKER"));

        Assert.Equal(CardHandKind.RandomCards, result.Kind);
    }

    private static object[] Case(CardHandKind kind, params string[] codes) => new object[] { kind, Cards(codes) };
}
