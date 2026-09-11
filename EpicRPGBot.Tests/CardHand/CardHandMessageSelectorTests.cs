using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardHandMessageSelectorTests
{
    [Fact]
    public void FindCandidates_PrefersNewRoundOverEditedConsumedRound()
    {
        var current = Prompt("chat-messages-1-100", "C6", "C2");
        var editedConsumed = Prompt("chat-messages-1-100", "C6", "C2", edited: true);
        var nextRound = Prompt("chat-messages-1-101", "C6", "C2", "DQ");

        var candidates = new CardHandMessageSelector().FindCandidates(
            new[] { editedConsumed, nextRound },
            current,
            3);

        Assert.Single(candidates);
        Assert.Equal(nextRound.Id, candidates[0].Id);
    }

    [Fact]
    public void FindCandidates_IgnoresTransientEditWithOldCardCount()
    {
        var current = Prompt("100", "C6", "C2");
        var editedConsumed = Prompt("100", "C6", "C2", edited: true);

        var candidates = new CardHandMessageSelector().FindCandidates(
            new[] { editedConsumed },
            current,
            3);

        Assert.Empty(candidates);
    }

    [Fact]
    public void FindCandidates_AcceptsSameMessageUpdatedToNextRound()
    {
        var current = Prompt("100", "C6", "C2");
        var updated = Prompt("100", "C6", "C2", "DQ");

        var candidates = new CardHandMessageSelector().FindCandidates(
            new[] { updated },
            current,
            3);

        Assert.Single(candidates);
        Assert.Equal(updated.Id, candidates[0].Id);
    }

    private static DiscordMessageSnapshot Prompt(string id, params string[] cardLabels)
    {
        return Prompt(id, cardLabels, false);
    }

    private static DiscordMessageSnapshot Prompt(string id, string first, string second, bool edited)
    {
        return Prompt(id, new[] { first, second }, edited);
    }

    private static DiscordMessageSnapshot Prompt(string id, string[] cardLabels, bool edited)
    {
        var buttons = cardLabels
            .Select((label, index) => new DiscordMessageButton(label, 0, index))
            .Append(new DiscordMessageButton("pass", 1, 0))
            .Append(new DiscordMessageButton("hands", 1, 1))
            .ToArray();
        var text = "Try to get the best possible card hand" + (edited ? " (edited)" : string.Empty);
        return new DiscordMessageSnapshot(id, text, "EPIC RPG", buttons: buttons);
    }
}
