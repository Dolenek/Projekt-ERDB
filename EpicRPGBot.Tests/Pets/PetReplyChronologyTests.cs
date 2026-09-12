using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Pets;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetReplyChronologyTests
{
    private const string Prefix = "chat-messages-1264330502005985391-";
    private const string Outgoing = Prefix + "1548258601900965888";
    private const string Stale = Prefix + "1548258597593550959";
    private const string Fresh = Prefix + "1548258601900965889";

    [Fact]
    public void ReportedReplyPredatesCommand() => Assert.False(PetReplyChronology.IsAfter(Stale, Outgoing));

    [Theory]
    [InlineData(Fresh, Outgoing, true)]
    [InlineData(Outgoing, Outgoing, false)]
    [InlineData("chat-messages-other-1548258601900965899", Outgoing, false)]
    [InlineData("invalid", Outgoing, false)]
    public void RequiresNewerMessageInSameChannel(string reply, string outgoing, bool expected) =>
        Assert.Equal(expected, PetReplyChronology.IsAfter(reply, outgoing));

    [Fact]
    public async Task WaitsForFreshReplyWithoutResendingReportedCommand()
    {
        var client = new FakePetChatClient { Current = new(Fresh, Page(Entry("A"), 1), "EPIC RPGVerified AppAPP") };
        var sends = 0;
        var gateway = new DiscordPetGateway(client, (command, token) => {
            sends++;
            return Task.FromResult(new ConfirmedCommandSendResult(new(Outgoing, command),
                new(Stale, Page(Entry("B"), 1), "EPIC RPGVerified AppAPP"), 1));
        }, "friendr");
        var inventory = await gateway.LoadAsync(default);
        Assert.Equal("A", Assert.Single(inventory.Pets).Id);
        Assert.Equal(1, sends);
    }

    [Fact]
    public async Task StaleReplyCannotBecomeSuccessfulOnCancellation()
    {
        var client = new FakePetChatClient { Current = new(Stale, Page(Entry("B"), 1), "EPIC RPGVerified AppAPP") };
        using var cancellation = new CancellationTokenSource(30);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => PetReplyChronology.WaitAsync(client, Outgoing, cancellation.Token));
    }
}
