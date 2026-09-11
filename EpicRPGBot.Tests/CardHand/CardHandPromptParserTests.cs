using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardHandPromptParserTests
{
    [Fact]
    public void Parse_UsesCardButtonsAndIgnoresHandsButton()
    {
        var snapshot = Message(
            Button("H2", 0),
            Button("DK", 1),
            Button("pass", 2),
            Button("hands", 3));

        var result = new CardHandPromptParser().Parse(snapshot);

        Assert.True(result.IsPrompt);
        Assert.True(result.IsValid);
        Assert.Equal(2, result.Cards.Count);
        Assert.Equal(2, result.DiscardButtons.Count);
        Assert.Equal("pass", result.PassButton.Label);
    }

    [Fact]
    public void Parse_RejectsDuplicateCardButtons()
    {
        var result = new CardHandPromptParser().Parse(Message(
            Button("H2", 0),
            Button("H2", 1),
            Button("pass", 2)));

        Assert.True(result.IsPrompt);
        Assert.False(result.IsValid);
        Assert.Contains("Duplicate", result.Error);
    }

    [Fact]
    public void Parse_RejectsUnexpectedActionButton()
    {
        var result = new CardHandPromptParser().Parse(Message(
            Button("H2", 0),
            Button("DK", 1),
            Button("mystery", 2),
            Button("pass", 3)));

        Assert.False(result.IsValid);
        Assert.NotNull(result.PassButton);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Parse_AcceptsEveryRoundPromptSize(int cardCount)
    {
        var labels = new[] { "H2", "DK", "SA", "C7" };
        var buttons = labels.Take(cardCount)
            .Select((label, index) => Button(label, index))
            .Append(Button("pass", cardCount))
            .ToArray();

        var result = new CardHandPromptParser().Parse(Message(buttons));

        Assert.True(result.IsValid, result.Error);
        Assert.Equal(cardCount, result.Cards.Count);
    }

    [Theory]
    [InlineData("Two pair | 83% reward — 4/5 owned, 0/5 goldened")]
    [InlineData("Ace Extravaganza")]
    [InlineData("Random Cards")]
    public void IsResult_RecognizesTerminalMessages(string text)
    {
        var snapshot = new DiscordMessageSnapshot("2", text, "EPIC RPG");
        Assert.True(new CardHandPromptParser().IsResult(snapshot));
    }

    private static DiscordMessageSnapshot Message(params DiscordMessageButton[] buttons)
    {
        return new DiscordMessageSnapshot("1", "Select a card or pass", "EPIC RPG", buttons: buttons);
    }

    private static DiscordMessageButton Button(string label, int column) => new(label, 0, column);
}
