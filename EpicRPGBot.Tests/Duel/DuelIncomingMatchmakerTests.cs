using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelIncomingMatchmakerTests
{
    [Fact]
    public async Task FallbackPostsExactCfOnceAndManualCleanupDeletesIt()
    {
        var client = new FakeDuelDiscordClient();
        var channels = CreateRequiredChannels();
        foreach (var channel in channels)
        {
            client.MessagesByUrl[channel.Url] = new List<DiscordMessageSnapshot>();
        }

        using var cancellation = new CancellationTokenSource();
        client.MessageSent = message =>
        {
            if (message == "142 cf")
            {
                cancellation.Cancel();
            }
        };
        var parser = new DuelMessageParser();
        var matchmaker = new DuelIncomingMatchmaker(
            client,
            new DuelChannelCatalog(),
            new DuelIncomingWatcher(client, parser),
            new DuelIncomingAttemptRunner(client, parser));
        var resumeCount = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => matchmaker.RunAsync(
            channels,
            new DuelProfileContext("firendr", 142, "100", "self"),
            () => Task.CompletedTask,
            () => { resumeCount++; return Task.CompletedTask; },
            _ => { },
            null,
            cancellation.Token));
        await matchmaker.CleanupOwnListingAsync(null);

        Assert.Equal(new[] { "142 cf" }, client.SentMessages);
        Assert.Equal(new[] { "sent-1" }, client.DeletedMessages);
        Assert.Equal(1, resumeCount);
    }

    [Fact]
    public async Task CleanupTreatsServerRemovedOwnOfferAsAlreadyComplete()
    {
        var client = new FakeDuelDiscordClient();
        var channels = CreateRequiredChannels();
        foreach (var channel in channels)
        {
            client.MessagesByUrl[channel.Url] = new List<DiscordMessageSnapshot>();
        }

        using var cancellation = new CancellationTokenSource();
        client.MessageSent = message =>
        {
            if (message == "142 cf") cancellation.Cancel();
        };
        var parser = new DuelMessageParser();
        var matchmaker = new DuelIncomingMatchmaker(
            client,
            new DuelChannelCatalog(),
            new DuelIncomingWatcher(client, parser),
            new DuelIncomingAttemptRunner(client, parser));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => matchmaker.RunAsync(
            channels,
            new DuelProfileContext("firendr", 142, "100", "self"),
            () => Task.CompletedTask,
            () => Task.CompletedTask,
            _ => { },
            null,
            cancellation.Token));
        client.MessagesByUrl["road200"].Clear();
        var reports = new List<string>();

        await matchmaker.CleanupOwnListingAsync(reports.Add);

        Assert.Empty(client.DeletedMessages);
        Assert.Contains(reports, message => message.Contains("already absent", StringComparison.Ordinal));
    }

    [Fact]
    public async Task IncomingRequestUsesChannelAlreadyOpenedByWatcher()
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(1);
        var client = new FakeDuelDiscordClient();
        var channels = CreateRequiredChannels();
        foreach (var channel in channels)
        {
            client.MessagesByUrl[channel.Url] = new List<DiscordMessageSnapshot>();
        }

        var duelChannel = channels.Single(channel => channel.Name == "dueling-1");
        client.MessageSent = message => AddIncomingDuelAfterListing(client, duelChannel, message, now);
        var parser = new DuelMessageParser();
        var matchmaker = new DuelIncomingMatchmaker(
            client,
            new DuelChannelCatalog(),
            new DuelIncomingWatcher(client, parser),
            new DuelIncomingAttemptRunner(client, parser));

        var result = await matchmaker.RunAsync(
            channels,
            new DuelProfileContext("firendr", 142, "100", "self"),
            () => Task.CompletedTask,
            () => Task.CompletedTask,
            _ => { },
            null,
            default);

        Assert.True(result.Completed);
        Assert.Equal(1, client.NavigatedUrls.Count(url => url == duelChannel.Url));
        Assert.Equal(new[] { ("request", "yes") }, client.ClickedButtons);
    }

    private static IReadOnlyList<DiscordChannelReference> CreateRequiredChannels()
    {
        var channels = new List<DiscordChannelReference>
        {
            new("road", "road-to-200", "road200")
        };
        channels.AddRange(Enumerable.Range(1, 4)
            .Select(index => new DiscordChannelReference(index.ToString(), $"dueling-{index}", $"duel{index}")));
        return channels;
    }

    private static void AddIncomingDuelAfterListing(
        FakeDuelDiscordClient client,
        DiscordChannelReference channel,
        string message,
        DateTimeOffset timestamp)
    {
        if (message != "142 cf") return;
        var requestText = "challenger — duel\nchallenger sent a Duel request to firendr.!\n" +
                          "Will you accept, firendr ? yes/no";
        client.MessagesByUrl[channel.Url].Add(DuelTestMessages.Message(
            "request",
            requestText,
            timestamp,
            buttons: new[] { DuelTestMessages.Button("yes"), DuelTestMessages.Button("no", 1) }));
        client.MessagesByUrl[channel.Url].Add(DuelTestMessages.Message(
            "result",
            "challenger — duel\nchallenger won!\nReward: 10 XP",
            timestamp.AddSeconds(1)));
        client.MentionCounts[channel.Id] = 1;
    }
}
