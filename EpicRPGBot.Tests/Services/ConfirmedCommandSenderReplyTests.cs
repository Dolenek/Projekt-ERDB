using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class ConfirmedCommandSenderReplyTests
{
    [Fact]
    public async Task UnrelatedRecentMessageCannotConfirmCommandBeforeCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var client = new ScriptedCommandChatClient();
        client.RecentMessages = new[]
        {
            client.Outgoing!,
            new DiscordMessageSnapshot(ScriptedCommandChatClient.MessagePrefix + "101", "hello", "Another player")
        };
        client.OnRecentMessagesRead = cancellation.Cancel;
        var sender = new ConfirmedCommandSender(client);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sender.SendAsync("rpg hunt", cancellationToken: cancellation.Token));

        Assert.Equal(1, client.SendCount);
    }

    [Theory]
    [MemberData(nameof(RejectedReplies))]
    public async Task InvalidDirectOrRecentReplyNeverConfirms(DiscordMessageSnapshot reply, bool direct)
    {
        var client = new ScriptedCommandChatClient
        {
            DirectReply = direct ? reply : null,
            RecentMessages = direct ? Array.Empty<DiscordMessageSnapshot>() : new[] { reply }
        };
        var result = await CreateFastSender(client).SendAsync("rpg hunt");

        Assert.False(result.IsConfirmed);
        Assert.Null(result.ReplyMessage);
        Assert.Equal(3, result.AttemptCount);
        Assert.Equal(3, client.SendCount);
    }

    [Theory]
    [InlineData("EPIC RPG", "result")]
    [InlineData("EPIC RPGVerified AppAPP", "result")]
    [InlineData("EPIC RPG ✓APP", "result")]
    [InlineData("\u200bEPIC\u00a0RPG\u200e", "result")]
    [InlineData("", "EPIC RPG\nAPP\n11:02\nresult")]
    public async Task ValidDiscordAuthorOrLeadingHeaderConfirms(string author, string text)
    {
        var reply = new DiscordMessageSnapshot(Prefix + "101", text, author);
        var client = new ScriptedCommandChatClient { DirectReply = reply };

        var result = await CreateFastSender(client).SendAsync("rpg hunt");

        Assert.True(result.IsConfirmed);
        Assert.Same(reply, result.ReplyMessage);
        Assert.Equal(1, client.SendCount);
    }

    [Fact]
    public async Task RecentMessagesUseEarliestFreshReplyEvenWithoutOutgoingInWindow()
    {
        var earliest = new DiscordMessageSnapshot(Prefix + "101", "first reply", "EPIC RPG");
        var client = new ScriptedCommandChatClient
        {
            RecentMessages = new[]
            {
                new DiscordMessageSnapshot(Prefix + "103", "later reply", "EPIC RPG"),
                new DiscordMessageSnapshot(Prefix + "99", "old reply", "EPIC RPG"), null!, earliest
            }
        };

        var result = await CreateFastSender(client).SendAsync("rpg hunt");

        Assert.True(result.IsConfirmed);
        Assert.Same(earliest, result.ReplyMessage);
    }

    [Fact]
    public async Task MissingAuthorMetadataCanUseRenderedLeadingHeader()
    {
        var reply = new DiscordMessageSnapshot(Prefix + "101", "result", "", "EPIC RPG APP\nresult");
        var client = new ScriptedCommandChatClient { RecentMessages = new[] { reply } };

        Assert.True((await CreateFastSender(client).SendAsync("rpg hunt")).IsConfirmed);
    }

    [Fact]
    public async Task RepeatedCommandTextCannotSelectAnOlderReply()
    {
        var client = new ScriptedCommandChatClient
        {
            RecentMessages = new[]
            {
                new DiscordMessageSnapshot(Prefix + "97", "rpg hunt", "Player"),
                new DiscordMessageSnapshot(Prefix + "98", "previous result", "EPIC RPG")
            }
        };

        Assert.False((await CreateFastSender(client).SendAsync("rpg hunt")).IsConfirmed);
    }

    [Theory]
    [InlineData("rpg dung @player")]
    [InlineData("rpg duel @player")]
    [InlineData("rpg pets fusion A B")]
    public async Task TimeoutDoesNotBlindlyResendProtectedCommands(string command)
    {
        var client = new ScriptedCommandChatClient();

        var result = await CreateFastSender(client).SendAsync(command);

        Assert.False(result.IsConfirmed);
        Assert.Equal(1, result.AttemptCount);
        Assert.Equal(1, client.SendCount);
        Assert.Equal(40, client.ReplyReadCount);
    }

    [Fact]
    public async Task CancelledPollingDoesNotConfirmOrRetry()
    {
        using var cancellation = new CancellationTokenSource();
        var client = new ScriptedCommandChatClient
        {
            DirectReply = new DiscordMessageSnapshot(Prefix + "101", "result", "EPIC RPG"),
            OnReplyRead = cancellation.Cancel
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateFastSender(client).SendAsync("rpg hunt", cancellationToken: cancellation.Token));

        Assert.Equal(1, client.SendCount);
    }

    [Fact]
    public async Task MissingOutgoingMessageDoesNotStartReplyPolling()
    {
        var client = new ScriptedCommandChatClient { Outgoing = null };

        var result = await CreateFastSender(client).SendAsync("rpg hunt");

        Assert.False(result.IsConfirmed);
        Assert.Equal(3, client.SendCount);
        Assert.Equal(0, client.ReplyReadCount);
    }

    public static IEnumerable<object[]> RejectedReplies()
    {
        var replies = new[]
        {
            new DiscordMessageSnapshot(Prefix + "101", "hello", "Another player"),
            new DiscordMessageSnapshot(Prefix + "101", "EPIC RPG\nArea: 15\nsuccessfully crafted", "Another player"),
            new DiscordMessageSnapshot(Prefix + "101", "result", "Fake EPIC RPG"),
            new DiscordMessageSnapshot(Prefix + "101", "player\nmentioned EPIC RPG", ""),
            new DiscordMessageSnapshot(Prefix + "101", "player is training in the forest in 15 seconds", ""),
            new DiscordMessageSnapshot(Prefix + "99", "old result", "EPIC RPG"),
            new DiscordMessageSnapshot(Prefix + "100", "same message", "EPIC RPG"),
            new DiscordMessageSnapshot("chat-messages-43-101", "other channel", "EPIC RPG"),
            new DiscordMessageSnapshot("invalid", "result", "EPIC RPG")
        };
        foreach (var reply in replies)
            foreach (var direct in new[] { true, false }) yield return new object[] { reply, direct };
    }

    private const string Prefix = ScriptedCommandChatClient.MessagePrefix;

    private static ConfirmedCommandSender CreateFastSender(ScriptedCommandChatClient client) => new(client, (_, token) =>
    {
        token.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    });
}
