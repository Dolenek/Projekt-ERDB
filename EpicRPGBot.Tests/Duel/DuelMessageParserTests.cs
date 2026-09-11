using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelMessageParserTests
{
    private readonly DuelMessageParser _parser = new();

    [Fact]
    public void Request_ExtractsInitiatorAndTarget()
    {
        var message = WithYes(
            "challenger — duel\nchallenger sent a Duel request to firendr.!\nWill you accept, firendr ? yes/no");

        var parsed = _parser.Parse(message);

        Assert.Equal(DuelMessageKind.IncomingRequest, parsed.Kind);
        Assert.Equal("challenger", parsed.InitiatorName);
        Assert.True(parsed.Targets("Firendr"));
    }

    [Fact]
    public void OutgoingRequestAddressedToOpponent_IsNotInitiatorConfirmation()
    {
        var message = WithYes(
            "firendr — duel\nfirendr sent a Duel request to opponent.!\nWill you accept, opponent ? yes/no");

        var parsed = _parser.Parse(message);

        Assert.Equal(DuelMessageKind.IncomingRequest, parsed.Kind);
        Assert.False(parsed.Targets("firendr"));
    }

    [Theory]
    [InlineData("firendr's Duel cancelled", DuelMessageKind.Cancelled)]
    [InlineData("firendr — duel\nfirendr won!\nReward: 123 XP", DuelMessageKind.Result)]
    [InlineData("firendr — duel\nIt's a tie!\nReward: 1 XP", DuelMessageKind.Result)]
    [InlineData("That user is currently busy in the middle of a command", DuelMessageKind.Busy)]
    [InlineData("That user is busy", DuelMessageKind.Busy)]
    [InlineData("That player is already in a duel", DuelMessageKind.Busy)]
    public void TerminalAndBusyMessages_AreClassified(string text, DuelMessageKind expected)
    {
        Assert.Equal(expected, _parser.Parse(DuelTestMessages.Message("1", text)).Kind);
    }

    [Fact]
    public void WeaponPrompt_IsClassified()
    {
        const string text = "firendr — duel\nfirendr, choose the weapon that better fits with you:";

        Assert.Equal(
            DuelMessageKind.WeaponChoice,
            _parser.Parse(DuelTestMessages.Message("1", text)).Kind);
    }

    private static DiscordMessageSnapshot WithYes(string text)
    {
        return DuelTestMessages.Message("1", text, buttons: new[] { DuelTestMessages.Button("yes") });
    }
}
