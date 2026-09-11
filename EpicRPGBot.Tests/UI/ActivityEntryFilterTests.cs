using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.UI;

public sealed class ActivityEntryFilterTests
{
    [Fact]
    public void MessageFilterAcceptsEmptyAndCaseInsensitiveQueries()
    {
        var message = new MessageItem("EPIC RPG cooldown snapshot received");

        Assert.True(ActivityEntryFilter.MatchesMessage(message, string.Empty));
        Assert.True(ActivityEntryFilter.MatchesMessage(message, "coolDOWN"));
        Assert.False(ActivityEntryFilter.MatchesMessage(message, "dungeon"));
    }

    [Fact]
    public void LogFilterCombinesKindAndCaseInsensitiveText()
    {
        var entry = new LogEntry(LogKind.Command, "Message (rpg hunt) sent");

        Assert.True(ActivityEntryFilter.MatchesLog(entry, "RPG HUNT", LogKind.Command));
        Assert.False(ActivityEntryFilter.MatchesLog(entry, "rpg hunt", LogKind.Warning));
        Assert.False(ActivityEntryFilter.MatchesLog(entry, "rpg farm", LogKind.Command));
    }

    [Theory]
    [InlineData(LogKind.Info)]
    [InlineData(LogKind.Command)]
    [InlineData(LogKind.Warning)]
    [InlineData(LogKind.Error)]
    [InlineData(LogKind.Engine)]
    public void LogFilterAcceptsEachExactKind(LogKind kind)
    {
        var entry = new LogEntry(kind, "status");

        Assert.True(ActivityEntryFilter.MatchesLog(entry, string.Empty, kind));
    }
}
