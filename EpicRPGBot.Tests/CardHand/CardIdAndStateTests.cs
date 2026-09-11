using EpicRPGBot.UI.CardHand;
using Xunit;
using static EpicRPGBot.Tests.CardHand.CardHandTestCards;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardIdAndStateTests
{
    [Fact]
    public void Catalog_ContainsFiftyTwoCardsAndOneJoker()
    {
        Assert.Equal(53, CardCatalog.All.Count);
        Assert.Equal(53, CardCatalog.All.Distinct().Count());
        Assert.Single(CardCatalog.All.Where(card => card.IsJoker));
    }

    [Theory]
    [InlineData("H2")]
    [InlineData("D10")]
    [InlineData("CJ")]
    [InlineData("SQ")]
    [InlineData("SA")]
    [InlineData("JOKER")]
    public void CardCodes_RoundTrip(string code)
    {
        Assert.True(CardId.TryParse(code, out var card));
        Assert.Equal(code, card.ToString());
    }

    [Fact]
    public void Pass_AddsOneCardWithoutDiscarding()
    {
        var state = new CardHandState(Cards("H2", "DK"));
        var next = state.Apply(CardHandAction.Pass, Cards("C4"));

        Assert.Equal(Cards("H2", "C4", "DK").OrderBy(card => card), next.Hand);
        Assert.Empty(next.Discarded);
    }

    [Fact]
    public void Discard_RemovesCardAndDrawsTwoUnseenCards()
    {
        var discarded = Card("DK");
        var state = new CardHandState(Cards("H2", "DK"));
        var next = state.Apply(CardHandAction.Discard(discarded), Cards("H4", "HA"));

        Assert.Equal(Cards("H2", "H4", "HA").OrderBy(card => card), next.Hand);
        Assert.Contains(discarded, next.Discarded);
        Assert.DoesNotContain(discarded, next.GetUnseenCards());
    }
}
