using EpicRPGBot.UI.Duel;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelAttemptRunnerTests
{
    [Fact]
    public async Task OutgoingDuel_IsSentOnceAndSelectsCreditCard()
    {
        var client = new FakeDuelDiscordClient();
        client.FollowupsAfterSend.Enqueue(new[]
        {
            DuelTestMessages.Message(
                "request",
                "firendr — duel\nWill you accept, opponent ?",
                buttons: new[] { DuelTestMessages.Button("yes") }),
            DuelTestMessages.Message(
                "weapon",
                "firendr — duel\nfirendr, choose the weapon that better fits with you:",
                buttons: new[] { DuelTestMessages.Button("🗡", 0), DuelTestMessages.Button("💳", 1) }),
            DuelTestMessages.Message("result", "firendr — duel\nopponent won!\nReward: 10 XP")
        });
        var runner = CreateRunner(client);
        var offer = new DuelOffer(
            new("1", "road-to-200", "road"),
            DuelTestMessages.Message("offer", "100 cf", authorId: "222"),
            100);

        var outcome = await runner.RunOutgoingAsync(offer, "firendr", null, null, default);

        Assert.Equal(DuelAttemptOutcome.Completed, outcome);
        Assert.Equal(new[] { "rpg duel 222" }, client.SentMessages);
        Assert.DoesNotContain(client.ClickedButtons, click => click.MessageId == "request");
        Assert.Contains(client.ClickedButtons, click => click == ("weapon", "💳"));
    }

    [Fact]
    public async Task OwnConfirmation_IsClickedBeforeResult()
    {
        var client = new FakeDuelDiscordClient();
        client.FollowupsAfterSend.Enqueue(new[]
        {
            DuelTestMessages.Message(
                "confirmation",
                "firendr — duel\nAre you sure you want to duel?",
                buttons: new[] { DuelTestMessages.Button("yes") }),
            DuelTestMessages.Message("result", "firendr — duel\nfirendr won!\nReward: 10 XP")
        });
        var runner = CreateRunner(client);
        var offer = new DuelOffer(
            new("1", "road-to-200", "road"),
            DuelTestMessages.Message("offer", "100 cf", authorId: "222"),
            100);

        var outcome = await runner.RunOutgoingAsync(offer, "firendr", null, null, default);

        Assert.Equal(DuelAttemptOutcome.Completed, outcome);
        Assert.Contains(client.ClickedButtons, click => click == ("confirmation", "yes"));
    }

    private static DuelAttemptRunner CreateRunner(FakeDuelDiscordClient client)
    {
        return new DuelAttemptRunner(client, new DuelMessageParser(), new DuelWeaponSelector());
    }
}
