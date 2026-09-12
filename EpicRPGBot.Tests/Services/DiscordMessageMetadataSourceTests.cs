using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class DiscordMessageMetadataSourceTests
{
    [Theory]
    [InlineData("DiscordChatClient.Messages.cs")]
    [InlineData("DiscordChatClient.Composer.cs")]
    public void SnapshotScriptsIncludeTabRoleAndChannelUrl(string fileName)
    {
        var source = File.ReadAllText(Path.Combine(
            GetRepositoryRoot(),
            "EpicRPGBot.UI",
            "Services",
            fileName));

        Assert.Contains("tabRole: window.__epicRpGBotTabRole || ''", source);
        Assert.Contains("channelUrl: window.location.href || ''", source);
    }

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
