using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelChannelCatalogTests
{
    private readonly DuelChannelCatalog _catalog = new();

    [Fact]
    public void Level80_UsesRoadTo50AndRoadTo200()
    {
        var names = _catalog.GetRelevantBands(80).Select(band => band.ChannelName);

        Assert.Equal(new[] { "road-to-50", "road-to-200" }, names);
    }

    [Theory]
    [InlineData(49, "road-to-50")]
    [InlineData(50, "road-to-200")]
    [InlineData(199, "road-to-200")]
    [InlineData(200, "road-to-500")]
    [InlineData(2999, "road-to-3000")]
    [InlineData(3000, "road-to-6000+")]
    public void PlayerBand_UsesExactBoundaryRules(int level, string expectedName)
    {
        Assert.Equal(expectedName, _catalog.GetPlayerBand(level).ChannelName);
    }

    [Theory]
    [InlineData(49, "road-to-50,road-to-200")]
    [InlineData(50, "road-to-50,road-to-200")]
    [InlineData(199, "road-to-200,road-to-500")]
    [InlineData(200, "road-to-200,road-to-500")]
    [InlineData(2999, "road-to-3000,road-to-6000+")]
    [InlineData(3000, "road-to-3000,road-to-6000+")]
    public void RelevantBands_IntersectRewardRangeAtBoundaries(int level, string expectedNames)
    {
        var actualNames = string.Join(",", _catalog.GetRelevantBands(level).Select(band => band.ChannelName));

        Assert.Equal(expectedNames, actualNames);
    }

    [Fact]
    public void MissingRequiredChannel_IsHardFailure()
    {
        var onlyRoadTo50 = new[] { new DiscordChannelReference("1", "road-to-50", "url") };

        Assert.Throws<InvalidOperationException>(() =>
            _catalog.ResolveDuelingChannels(onlyRoadTo50));
    }

    [Fact]
    public void Dueling2WithUnexpectedId_IsHardFailure()
    {
        var wrongChannel = new[] { new DiscordChannelReference("wrong", "dueling-2", "url") };

        Assert.Throws<InvalidOperationException>(() =>
            _catalog.ResolveOutgoingDuelChannel(wrongChannel));
    }

    [Fact]
    public void FixedRoadChannelsOverrideIncompleteDiscordDiscovery()
    {
        var discovered = new[]
        {
            new DiscordChannelReference("stale", "road-to-200", "stale-url"),
            new DiscordChannelReference("1", "dueling-1", "duel1")
        };

        var merged = _catalog.MergeFixedChannels(discovered);
        var roadTo200 = merged.Single(channel => channel.Name == "road-to-200");

        Assert.Equal("1262468343135342703", roadTo200.Id);
        Assert.Equal(
            "https://discord.com/channels/792124157117988907/1262468343135342703",
            roadTo200.Url);
        Assert.Contains(merged, channel => channel.Name == "dueling-1");
    }

    [Theory]
    [InlineData("https://discord.com/channels/792124157117988907/1262468343135342703", true)]
    [InlineData("https://discord.com/channels/792124157117988907/792125562557562880", true)]
    [InlineData("https://discord.com/channels/792124157117988907/792125583374024754", true)]
    [InlineData("https://discord.com/channels/792124157117988907/1036018255812366377", true)]
    [InlineData("https://discord.com/channels/792124157117988907/1062896821950742638", true)]
    [InlineData("https://discord.com/channels/111/792125562557562880", false)]
    [InlineData("https://discord.com/channels/792124157117988907/999", false)]
    [InlineData("https://example.com/channels/792124157117988907/792125562557562880", false)]
    public void TargetUrl_AllowsOnlyFixedDuelScope(string url, bool expected)
    {
        Assert.Equal(expected, DuelChannelCatalog.IsAllowedTargetUrl(url));
    }

    [Fact]
    public void TargetDescription_ContainsFixedChannelNameAndId()
    {
        const string url = "https://discord.com/channels/792124157117988907/1036018255812366377";

        Assert.Equal("#dueling-3 (1036018255812366377)", DuelChannelCatalog.DescribeTargetUrl(url));
    }
}
