using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class DiscordMessagePermalinkTests
{
    [Theory]
    [InlineData(
        "https://discord.com/channels/111111111111111/222222222222222",
        "https://discord.com/channels/111111111111111/222222222222222/333333333333333")]
    [InlineData(
        "https://discord.com/channels/@me/222222222222222/999999999999999",
        "https://discord.com/channels/@me/222222222222222/333333333333333")]
    public void TryBuild_NormalizesGuildAndDirectMessageLinks(string channelUrl, string expected)
    {
        var reference = new DiscordMessageReference(
            "chat-messages-333333333333333",
            DiscordTabRole.Bot,
            channelUrl);

        var built = DiscordMessagePermalink.TryBuild(reference, out var permalink);

        Assert.True(built);
        Assert.Equal(expected, permalink);
    }

    [Fact]
    public void TryBuild_ExtractsMessageIdFromDiscordChannelAndMessageDomId()
    {
        var reference = new DiscordMessageReference(
            "chat-messages-222222222222222-333333333333333",
            DiscordTabRole.Bot,
            "https://discord.com/channels/111111111111111/222222222222222");

        Assert.True(DiscordMessagePermalink.TryBuild(reference, out var permalink));
        Assert.EndsWith("/333333333333333", permalink);
    }

    [Theory]
    [InlineData("https://example.com/channels/111111111111111/222222222222222", "chat-messages-333333333333333")]
    [InlineData("https://evil.discord.com/channels/111111111111111/222222222222222", "chat-messages-333333333333333")]
    [InlineData("http://discord.com/channels/111111111111111/222222222222222", "chat-messages-333333333333333")]
    [InlineData("https://discord.com/app", "chat-messages-333333333333333")]
    [InlineData("https://discord.com/channels/111111111111111/222222222222222", "message-without-id")]
    [InlineData("https://discord.com/channels/111111111111111/222222222222222", "prefix-333333333333333")]
    public void TryBuild_RejectsInvalidTargets(string channelUrl, string messageElementId)
    {
        var reference = new DiscordMessageReference(messageElementId, DiscordTabRole.Bot, channelUrl);

        Assert.False(DiscordMessagePermalink.TryBuild(reference, out var permalink));
        Assert.Equal(string.Empty, permalink);
    }
}
