using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelOfferParserTests
{
    private readonly DuelOfferParser _parser = new();
    private readonly DateTimeOffset _now = new(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("80 cf", 80)]
    [InlineData("80 cf gxp", 80)]
    [InlineData("hello\n100 CF 2 gxp okay", 100)]
    public void EligibleCf_IsAccepted(string text, int expectedLevel)
    {
        var parsed = Parse(text, opponentLevel: out var level);

        Assert.True(parsed);
        Assert.Equal(expectedLevel, level);
    }

    [Theory]
    [InlineData("80 nf")]
    [InlineData("80 gxp")]
    [InlineData("80 cf nf")]
    public void NonCfOffer_IsRejected(string text)
    {
        Assert.False(Parse(text, opponentLevel: out _));
    }

    [Fact]
    public void ThousandsSeparator_IsAccepted()
    {
        var message = DuelTestMessages.Message("1", "1,234 cf", _now);

        var parsed = _parser.TryParseEligibleCf(
            message,
            1000,
            _now.AddMinutes(-15),
            "100",
            "self",
            out var level);

        Assert.True(parsed);
        Assert.Equal(1234, level);
    }

    [Fact]
    public void ExactlyFifteenMinutesOld_IsAccepted()
    {
        var message = DuelTestMessages.Message("1", "80 cf", _now.AddMinutes(-15));

        Assert.True(_parser.TryParseEligibleCf(message, 80, _now.AddMinutes(-15), "100", "self", out _));
    }

    [Fact]
    public void OlderThanFifteenMinutes_IsRejected()
    {
        var message = DuelTestMessages.Message("1", "80 cf", _now.AddMinutes(-15).AddTicks(-1));

        Assert.False(_parser.TryParseEligibleCf(message, 80, _now.AddMinutes(-15), "100", "self", out _));
    }

    [Theory]
    [InlineData("white_check_mark")]
    [InlineData(":white_check_mark:")]
    [InlineData("✅")]
    [InlineData("AFK")]
    [InlineData(":AFK:")]
    public void ClaimedOrAfkOffer_IsRejected(string reactionName)
    {
        var reactions = new[] { new DiscordMessageReaction(reactionName, false, 1) };
        var message = DuelTestMessages.Message("1", "80 cf", _now, reactions: reactions);

        Assert.False(_parser.TryParseEligibleCf(message, 80, _now.AddMinutes(-15), "100", "self", out _));
    }

    [Fact]
    public void OwnOrUnidentifiedOffer_IsRejected()
    {
        var own = DuelTestMessages.Message("1", "80 cf", _now, authorId: "100");
        var unidentified = DuelTestMessages.Message("2", "80 cf", _now, authorId: string.Empty);

        Assert.False(_parser.TryParseEligibleCf(own, 80, _now.AddMinutes(-15), "100", "self", out _));
        Assert.False(_parser.TryParseEligibleCf(unidentified, 80, _now.AddMinutes(-15), "100", "self", out _));
    }

    [Theory]
    [InlineData(39, false)]
    [InlineData(40, true)]
    [InlineData(160, true)]
    [InlineData(161, false)]
    public void Eligibility_IsInclusiveFromHalfToDouble(int opponentLevel, bool expected)
    {
        Assert.Equal(expected, Parse($"{opponentLevel} cf", opponentLevel: out _));
    }

    private bool Parse(string text, out int opponentLevel)
    {
        var message = DuelTestMessages.Message("1", text, _now);
        return _parser.TryParseEligibleCf(
            message,
            80,
            _now.AddMinutes(-15),
            "100",
            "self",
            out opponentLevel);
    }
}
