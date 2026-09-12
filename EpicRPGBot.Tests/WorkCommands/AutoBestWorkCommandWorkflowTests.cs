using System.Collections.Generic;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.WorkCommands;
using Xunit;

namespace EpicRPGBot.Tests.WorkCommands;

public sealed class AutoBestWorkCommandWorkflowTests
{
    private const string ProfileReply = @"firendr — profile
Time travels: 37
Coins: 10,000,000
Bank: 25,000,000";
    private const string ProfessionReply = "firendr — professions\nWorker Lv 104";
    private const string BothPotionsReply = @"firendr — boosts
These are your active boosts
Fish potion: 1h
Wood potion: 1h";

    [Fact]
    public async Task RegularRun_OnlyLoadsProfile()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = ProfileReply
        });

        var result = await workflow.RunAsync(false, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(AutoBestWorkCommandMode.Regular, result.Mode);
        Assert.Equal(new[] { "rpg p" }, sentCommands);
    }

    [Fact]
    public async Task AscendedRun_LoadsAllInputsAndUsesBothPotionsTable()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = ProfileReply,
            ["rpg pr"] = ProfessionReply,
            ["rpg boost"] = BothPotionsReply
        });

        var result = await workflow.RunAsync(true, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(result.UsedFallback);
        Assert.Equal(AutoBestWorkCommandMode.AscendedWithPotions, result.Mode);
        Assert.Equal(new[] { "rpg p", "rpg pr", "rpg boost" }, sentCommands);
    }

    [Fact]
    public async Task MissingWorker_UsesRegularFallbackWithoutLoadingBoosts()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = ProfileReply,
            ["rpg pr"] = "firendr — professions"
        });

        var result = await workflow.RunAsync(true, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal(AutoBestWorkCommandMode.Regular, result.Mode);
        Assert.Equal(new[] { "rpg p", "rpg pr" }, sentCommands);
    }

    [Fact]
    public async Task ProfessionQueryException_UsesRegularFallback()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = ProfileReply
        }, "rpg pr");

        var result = await workflow.RunAsync(true, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal(AutoBestWorkCommandMode.Regular, result.Mode);
    }

    [Fact]
    public async Task InvalidBoost_UsesNoPotionsTable()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = ProfileReply,
            ["rpg pr"] = ProfessionReply,
            ["rpg boost"] = "unrelated reply"
        });

        var result = await workflow.RunAsync(true, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal(AutoBestWorkCommandMode.AscendedWithoutPotions, result.Mode);
        Assert.Equal("rpg greenhouse", result.Selections[6]);
    }

    [Fact]
    public async Task BoostQueryException_UsesNoPotionsTable()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = ProfileReply,
            ["rpg pr"] = ProfessionReply
        }, "rpg boost");

        var result = await workflow.RunAsync(true, null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal(AutoBestWorkCommandMode.AscendedWithoutPotions, result.Mode);
    }

    [Fact]
    public async Task InvalidProfile_FailsWithoutLoadingOtherInputs()
    {
        var sentCommands = new List<string>();
        var workflow = CreateWorkflow(sentCommands, new Dictionary<string, string>
        {
            ["rpg p"] = "firendr — profile"
        });

        var result = await workflow.RunAsync(true, null, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Empty(result.Selections);
        Assert.Equal(new[] { "rpg p" }, sentCommands);
    }

    [Fact]
    public async Task Cancellation_PropagatesWithoutFallback()
    {
        var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var workflow = CreateWorkflow(new List<string>(), new Dictionary<string, string>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            workflow.RunAsync(true, null, cancellation.Token));
    }

    private static AutoBestWorkCommandWorkflow CreateWorkflow(
        ICollection<string> sentCommands,
        IReadOnlyDictionary<string, string> replies,
        string commandThatThrows = null)
    {
        return new AutoBestWorkCommandWorkflow(
            (command, onOutgoing, token) =>
            {
                token.ThrowIfCancellationRequested();
                sentCommands.Add(command);
                if (string.Equals(command, commandThatThrows, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Simulated query failure.");
                }

                var outgoing = new DiscordMessageSnapshot("out-" + command, command, "Firender");
                onOutgoing?.Invoke(outgoing);
                replies.TryGetValue(command, out var replyText);
                var reply = replyText == null
                    ? null
                    : new DiscordMessageSnapshot("reply-" + command, replyText, "EPIC RPG");
                return Task.FromResult(new ConfirmedCommandSendResult(outgoing, reply, 1));
            },
            (milliseconds, token) => Task.CompletedTask);
    }
}
