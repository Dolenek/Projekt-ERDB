using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class InMemoryLogTests
{
    [Fact]
    public void ContextualEntry_PreservesReferenceAndRenderedText()
    {
        var log = new InMemoryLog();
        var reference = new DiscordMessageReference(
            "chat-messages-333333333333333",
            DiscordTabRole.Bot,
            "https://discord.com/channels/111111111111111/222222222222222");

        log.Command("Message (rpg hunt) sent", reference);

        var entry = Assert.Single(log.Items);
        Assert.True(entry.CanNavigate);
        Assert.Same(reference, entry.MessageReference);
        Assert.True(ActivityEntryFilter.MatchesLog(entry, "RPG HUNT", LogKind.Command));
        Assert.Same(reference, entry.MessageReference);
        Assert.EndsWith("Command: Message (rpg hunt) sent", entry.ToString());
    }

    [Fact]
    public void PlainEntry_RemainsNonNavigable()
    {
        var log = new InMemoryLog();

        log.Engine("UI loaded");

        Assert.False(Assert.Single(log.Items).CanNavigate);
    }

    [Fact]
    public void ContextualEntries_KeepExistingFiveHundredLineLimit()
    {
        var log = new InMemoryLog();
        var reference = new DiscordMessageReference(
            "chat-messages-333333333333333",
            DiscordTabRole.Bot,
            "https://discord.com/channels/111111111111111/222222222222222");

        for (var index = 0; index <= 500; index++)
        {
            log.Info("line-" + index, reference);
        }

        Assert.Equal(500, log.Items.Count);
        Assert.Equal("line-1", log.Items[0].Message);
        Assert.True(log.Items[0].CanNavigate);
    }
}
