using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class GuildRaidWatcherPolicyTests
{
    [Fact]
    public void InactiveWatcherDoesNotRunWithCompleteConfiguration()
    {
        var settings = CreateConfiguredSettings().WithGuildRaidWatcherActive(false);

        Assert.False(GuildRaidWatcherPolicy.ShouldRun(settings));
    }

    [Fact]
    public void ActiveWatcherRequiresCompleteValidConfiguration()
    {
        var incomplete = AppSettingsSnapshot.Default.WithGuildRaidWatcherActive(true);
        var invalidUrl = incomplete
            .WithGuildRaidChannelUrl("https://example.com/channels/1/2")
            .WithGuildRaidTriggerText("raid");

        Assert.False(GuildRaidWatcherPolicy.ShouldRun(incomplete));
        Assert.False(GuildRaidWatcherPolicy.ShouldRun(invalidUrl));
        Assert.True(GuildRaidWatcherPolicy.ShouldRun(CreateConfiguredSettings()));
    }

    private static AppSettingsSnapshot CreateConfiguredSettings()
    {
        return AppSettingsSnapshot.Default
            .WithGuildRaidChannelUrl("https://discord.com/channels/123/456")
            .WithGuildRaidTriggerText("raid")
            .WithGuildRaidWatcherActive(true);
    }
}
