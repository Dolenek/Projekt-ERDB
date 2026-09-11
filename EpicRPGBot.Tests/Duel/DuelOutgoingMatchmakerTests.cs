using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelOutgoingMatchmakerTests
{
    [Fact]
    public async Task CancelledOffer_IsMarkedAfkBeforeNextFifoOffer()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(now);
        client.FollowupsAfterSend.Enqueue(new[]
        {
            DuelTestMessages.Message("cancel", "firendr's Duel cancelled")
        });
        client.FollowupsAfterSend.Enqueue(new[]
        {
            DuelTestMessages.Message("result", "firendr — duel\nfirendr won!\nReward: 10 XP")
        });
        var matcher = CreateMatchmaker(client, now);
        var pauseCount = 0;
        var resumeCount = 0;

        var result = await matcher.RunAsync(
            client.DiscoveredChannels.ToArray(),
            new DuelProfileContext("firendr", 80, "100", "self"),
            () => { pauseCount++; return Task.CompletedTask; },
            () => { resumeCount++; return Task.CompletedTask; },
            _ => { },
            null,
            default);

        Assert.True(result?.Completed);
        Assert.Equal(new[] { "rpg duel 201", "rpg duel 202" }, client.SentMessages);
        Assert.Equal(2, pauseCount);
        Assert.Equal(1, resumeCount);
        Assert.Contains(client.AddedReactions, reaction => reaction == ("older", "AFK"));
    }

    [Fact]
    public async Task FailedClaimReaction_SkipsOfferWithoutPausingOrSending()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(now, includeSecondOffer: false);
        client.FailedReactionMessages.Add("older");
        var matcher = CreateMatchmaker(client, now);
        var pauseCount = 0;

        var result = await matcher.RunAsync(
            client.DiscoveredChannels.ToArray(),
            new DuelProfileContext("firendr", 80, "100", "self"),
            () => { pauseCount++; return Task.CompletedTask; },
            () => Task.CompletedTask,
            _ => { },
            null,
            default);

        Assert.Null(result);
        Assert.Equal(0, pauseCount);
        Assert.Empty(client.SentMessages);
    }

    [Fact]
    public async Task BusyResponseKeepsClaimWithoutAfkAndResumesEngine()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(now, includeSecondOffer: false);
        client.FollowupsAfterSend.Enqueue(new[]
        {
            DuelTestMessages.Message("busy", "That user is currently busy in the middle of a command")
        });
        var matcher = CreateMatchmaker(client, now);
        var resumeCount = 0;

        var result = await matcher.RunAsync(
            client.DiscoveredChannels.ToArray(),
            new DuelProfileContext("firendr", 80, "100", "self"),
            () => Task.CompletedTask,
            () => { resumeCount++; return Task.CompletedTask; },
            _ => { },
            null,
            default);

        Assert.Null(result);
        Assert.Equal(1, resumeCount);
        Assert.Contains(client.AddedReactions, reaction => reaction == ("older", "white_check_mark"));
        Assert.DoesNotContain(client.AddedReactions, reaction => reaction.Emoji == "AFK");
    }

    [Fact]
    public async Task UncertainOutgoingRegistrationRequiresBotToRemainStopped()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(now, includeSecondOffer: false);
        client.UncertainOutgoingCommands.Add("rpg duel 201");
        var matcher = CreateMatchmaker(client, now);
        var challengeState = false;
        var resumeCount = 0;

        var result = await matcher.RunAsync(
            client.DiscoveredChannels.ToArray(),
            new DuelProfileContext("firendr", 80, "100", "self"),
            () => Task.CompletedTask,
            () => { resumeCount++; return Task.CompletedTask; },
            value => challengeState = value,
            null,
            default);

        Assert.NotNull(result);
        Assert.True(result.RequiresBotToRemainStopped);
        Assert.True(challengeState);
        Assert.Equal(0, resumeCount);
        Assert.Equal(new[] { "rpg duel 201" }, client.SentMessages);
    }

    [Fact]
    public async Task CancellationBeforeChallengeReleasesClaim()
    {
        var now = DateTimeOffset.UtcNow;
        var client = CreateClient(now, includeSecondOffer: false);
        var matcher = CreateMatchmaker(client, now);

        await Assert.ThrowsAsync<OperationCanceledException>(() => matcher.RunAsync(
            client.DiscoveredChannels.ToArray(),
            new DuelProfileContext("firendr", 80, "100", "self"),
            () => throw new OperationCanceledException(),
            () => Task.CompletedTask,
            _ => { },
            null,
            default));

        Assert.Contains(client.RemovedReactions, reaction => reaction == ("older", "white_check_mark"));
        Assert.Empty(client.SentMessages);
    }

    private static DuelOutgoingMatchmaker CreateMatchmaker(FakeDuelDiscordClient client, DateTimeOffset now)
    {
        var catalog = new DuelChannelCatalog();
        var scanner = new DuelOfferScanner(client, catalog, new DuelOfferParser(), () => now);
        var runner = new DuelAttemptRunner(client, new DuelMessageParser(), new DuelWeaponSelector());
        return new DuelOutgoingMatchmaker(client, scanner, runner);
    }

    private static FakeDuelDiscordClient CreateClient(DateTimeOffset now, bool includeSecondOffer = true)
    {
        var client = new FakeDuelDiscordClient();
        var road50 = new DiscordChannelReference("1", "road-to-50", "road50");
        var road200 = new DiscordChannelReference("2", "road-to-200", "road200");
        client.DiscoveredChannels.Add(road50);
        client.DiscoveredChannels.Add(road200);
        client.MessagesByUrl[road50.Url] = new List<DiscordMessageSnapshot>
        {
            DuelTestMessages.Message("older", "42 cf", now.AddMinutes(-4), authorId: "201")
        };
        client.MessagesByUrl[road200.Url] = includeSecondOffer
            ? new List<DiscordMessageSnapshot>
            {
                DuelTestMessages.Message("newer", "100 cf", now.AddMinutes(-2), authorId: "202")
            }
            : new List<DiscordMessageSnapshot>();
        return client;
    }
}
