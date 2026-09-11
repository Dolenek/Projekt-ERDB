using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class DiscordScriptParsingTests
{
    [Fact]
    public void ExtendedSnapshotFields_AreParsedWithoutBreakingExistingFields()
    {
        const string json = """
            {"id":"1","text":"80 cf","author":"opponent","authorId":"123",
             "createdAtUtc":"2026-09-11T08:30:00.000Z",
             "reactions":[{"name":"white_check_mark","isMine":true,"count":2}]}
            """;

        var snapshot = DiscordScriptParsing.ParseSnapshot(json);

        Assert.Equal("1", snapshot?.Id);
        Assert.Equal("123", snapshot?.AuthorId);
        Assert.Equal(new DateTimeOffset(2026, 9, 11, 8, 30, 0, TimeSpan.Zero), snapshot?.CreatedAtUtc);
        var reaction = Assert.Single(snapshot!.Reactions);
        Assert.Equal("white_check_mark", reaction.Name);
        Assert.True(reaction.IsMine);
        Assert.Equal(2, reaction.Count);
    }

    [Fact]
    public void LegacySnapshotWithoutNewFields_UsesSafeDefaults()
    {
        var snapshot = DiscordScriptParsing.ParseSnapshot("{\"id\":\"1\",\"text\":\"legacy\"}");

        Assert.Equal(string.Empty, snapshot?.AuthorId);
        Assert.Null(snapshot?.CreatedAtUtc);
        Assert.Empty(snapshot!.Reactions);
    }

    [Fact]
    public void ChannelMentionCounts_ParseNumericAndStringValues()
    {
        var counts = DiscordChannelMentionParser.Parse("{\"one\":2,\"two\":\"3\",\"bad\":null}");

        Assert.Equal(2, counts["one"]);
        Assert.Equal(3, counts["two"]);
        Assert.False(counts.ContainsKey("bad"));
    }
}
