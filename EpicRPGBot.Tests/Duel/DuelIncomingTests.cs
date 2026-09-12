using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelIncomingTests
{
    [Fact]
    public async Task WatcherVisitsOnlyMentionedChannelsAndReturnsGlobalOldestRequest()
    {
        var now = DateTimeOffset.UtcNow;
        var client = new FakeDuelDiscordClient();
        var channels = CreateDuelingChannels();
        foreach (var channel in channels)
        {
            client.MessagesByUrl[channel.Url] = new List<DiscordMessageSnapshot>();
        }

        client.MessagesByUrl[channels[2].Url].Add(Request("older", "firendr", now.AddSeconds(1)));
        client.MessagesByUrl[channels[0].Url].Add(Request("wrong-target", "somebody", now));
        client.MessagesByUrl[channels[3].Url].Add(Request("newer", "firendr", now.AddSeconds(2)));
        client.MentionCounts[channels[2].Id] = 1;
        client.MentionCounts[channels[3].Id] = 1;
        var watcher = new DuelIncomingWatcher(client, new DuelMessageParser());
        var mentionBaseline = channels.ToDictionary(channel => channel.Id, _ => 0);
        var parkingChannel = new DiscordChannelReference("road", "road-to-200", "road200");

        var request = await watcher.WaitForRequestAsync(
            channels,
            parkingChannel,
            mentionBaseline,
            "firendr",
            now,
            new HashSet<string>(),
            null,
            default);

        Assert.Equal("older", request.Classification.Message.Id);
        Assert.Equal("dueling-3", request.Channel.Name);
        Assert.Equal(new[] { channels[2].Url, channels[3].Url, channels[2].Url }, client.NavigatedUrls);
    }

    [Fact]
    public async Task AcceptedIncomingDuel_DoesNotChooseAWeapon()
    {
        var now = DateTimeOffset.UtcNow;
        var client = new FakeDuelDiscordClient();
        var channel = CreateDuelingChannels()[0];
        await client.NavigateToChannelAsync(channel.Url);
        var prompt = Request("request", "firendr", now);
        client.MessagesByUrl[channel.Url].Add(prompt);
        client.MessagesByUrl[channel.Url].Add(DuelTestMessages.Message(
            "weapon",
            "firendr — duel\nfirendr, choose the weapon that better fits with you:",
            now,
            buttons: new[] { DuelTestMessages.Button("💳") }));
        client.MessagesByUrl[channel.Url].Add(DuelTestMessages.Message(
            "result",
            "challenger — duel\nchallenger won!\nReward: 10 XP",
            now));
        var classification = new DuelMessageParser().Parse(prompt);
        var runner = new DuelIncomingAttemptRunner(client, new DuelMessageParser());

        var outcome = await runner.AcceptAndWaitAsync(
            new DuelIncomingRequest(channel, classification),
            "firendr",
            null,
            default);

        Assert.Equal(DuelAttemptOutcome.Completed, outcome);
        Assert.Equal(new[] { ("request", "yes") }, client.ClickedButtons);
    }

    [Fact]
    public async Task WatcherIgnoresExistingBadgeUntilMentionCountIncreases()
    {
        var now = DateTimeOffset.UtcNow;
        var client = new FakeDuelDiscordClient();
        var channels = CreateDuelingChannels();
        foreach (var channel in channels)
        {
            client.MessagesByUrl[channel.Url] = new List<DiscordMessageSnapshot>();
        }

        client.MessagesByUrl[channels[0].Url].Add(Request("new-request", "firendr", now));
        client.MentionCounts[channels[0].Id] = 1;
        var watcher = new DuelIncomingWatcher(client, new DuelMessageParser());
        var baseline = await watcher.CaptureMentionBaselineAsync(channels, default);
        client.MentionCountSnapshots.Enqueue(new Dictionary<string, int> { [channels[0].Id] = 1 });
        client.MentionCountSnapshots.Enqueue(new Dictionary<string, int> { [channels[0].Id] = 2 });

        var request = await watcher.WaitForRequestAsync(
            channels,
            new DiscordChannelReference("road", "road-to-200", "road200"),
            baseline,
            "firendr",
            now,
            new HashSet<string>(),
            null,
            default);

        Assert.Equal("new-request", request.Classification.Message.Id);
        Assert.Equal(new[] { channels[0].Url }, client.NavigatedUrls);
    }

    [Fact]
    public void MentionStateDoesNotReopenChannelUntilBadgeIsStablyCleared()
    {
        var channel = CreateDuelingChannels()[0];
        var channels = new[] { channel };
        var baseline = new Dictionary<string, int> { [channel.Id] = 0 };
        var state = new DuelMentionState();

        Assert.Single(state.FindIncreases(channels, Counts(channel, 1), baseline));
        Assert.Empty(state.FindIncreases(channels, Counts(channel, 0), baseline));
        Assert.Empty(state.FindIncreases(channels, Counts(channel, 1), baseline));
        Assert.Empty(state.FindIncreases(channels, Counts(channel, 0), baseline));
        Assert.Empty(state.FindIncreases(channels, Counts(channel, 0), baseline));
        Assert.Empty(state.FindIncreases(channels, Counts(channel, 0), baseline));
        Assert.Single(state.FindIncreases(channels, Counts(channel, 1), baseline));
    }

    private static DiscordMessageSnapshot Request(string id, string target, DateTimeOffset timestamp)
    {
        var text = $"challenger — duel\nchallenger sent a Duel request to {target}.!\n" +
                   $"Will you accept, {target} ? yes/no";
        return DuelTestMessages.Message(
            id,
            text,
            timestamp,
            buttons: new[] { DuelTestMessages.Button("yes"), DuelTestMessages.Button("no", 1) });
    }

    private static IReadOnlyList<DiscordChannelReference> CreateDuelingChannels()
    {
        return Enumerable.Range(1, 4)
            .Select(index => new DiscordChannelReference(index.ToString(), $"dueling-{index}", $"duel{index}"))
            .ToArray();
    }

    private static IReadOnlyDictionary<string, int> Counts(
        DiscordChannelReference channel,
        int count) => new Dictionary<string, int> { [channel.Id] = count };
}
