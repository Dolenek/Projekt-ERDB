using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Pets;
using EpicRPGBot.UI.Services;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class DiscordPetGatewayTests
{
    [Fact]
    public async Task LoadsInventoryWithDiscordAppBadgeInAuthor()
    {
        var client = new FakePetChatClient { Current = new("1", Page(Entry("A"), 1), "EPIC RPGAPP") };
        var inventory = await Gateway(client).LoadAsync(default);
        Assert.Single(inventory.Pets);
    }

    [Theory]
    [InlineData("▶️")]
    [InlineData("epic_arrow_right")]
    public async Task ReadsEditedSameMessageAcrossPages(string nextLabel)
    {
        var client = new FakePetChatClient {
            Current = new("1", Page(Entry("A"), 2, 1, 2), "EPIC RPG", buttons: new[] { new DiscordMessageButton(nextLabel, 0, 2) }),
            NextPage = new("1", Page(Entry("B"), 2, 2, 2), "EPIC RPG")
        };
        var inventory = await Gateway(client).LoadAsync(default);
        Assert.True(inventory.Complete);
        Assert.Equal(2, inventory.Pets.Count);
        Assert.Equal(1, client.Clicks);
    }

    [Theory]
    [InlineData(3, "B")] [InlineData(2, "A")]
    public async Task RejectsIncompleteOrDuplicateInventory(int total, string secondId)
    {
        var client = new FakePetChatClient { Current = new("1", Page(Entry("A") + Entry(secondId), total), "EPIC RPG") };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Gateway(client).LoadAsync(default));
    }

    [Theory]
    [InlineData("someone else", "friendr")]
    [InlineData("EPIC RPG", "another player")]
    public async Task RejectsWrongAuthorOrOwner(string author, string owner)
    {
        var client = new FakePetChatClient { Current = new("1", Page(Entry("A"), 1).Replace("friendr", owner), author) };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Gateway(client).LoadAsync(default));
    }

    [Theory]
    [InlineData("Please wait at least 1s before fusion.")]
    [InlineData("You must end your previous command.")]
    [InlineData("Unknown message")]
    public async Task UnrecognizedFusionReplyIsNeverRetried(string response)
    {
        var client = new FakePetChatClient { Current = new("1", response, "EPIC RPG") };
        var sends = 0;
        var gateway = new DiscordPetGateway(client, (command, token) => {
            sends++;
            return Task.FromResult(new ConfirmedCommandSendResult(new("0", command), client.Current, 1));
        }, "friendr");
        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.FuseAsync(new PetFusionStep { Parents = new[] { Pet("A"), Pet("B") } }));
        Assert.Equal(1, sends);
    }

    [Theory]
    [InlineData("rpg pets fusion A B")]
    [InlineData(" RPG   PETS   FUSION   A B ")]
    [InlineData("rpg pets fusion")]
    public void FusionCannotBlindResend(string command) => Assert.False(DiscordCommandSendPolicy.AllowsBlindResend(command));

    [Fact]
    public async Task RecognizedFusionReplyAllowsInventoryReconciliation()
    {
        var client = new FakePetChatClient { Current = new("1", "friendr — pets\nYou have got a new pet!\nID: J\nCat — TIER VI\nFast [F]\nHappy [F]", "EPIC RPG") };
        await Gateway(client).FuseAsync(new PetFusionStep { Parents = new[] { Pet("A"), Pet("B") } });
    }

    private static DiscordPetGateway Gateway(FakePetChatClient client) => new(client,
        (command, token) => Task.FromResult(new ConfirmedCommandSendResult(new("0", command), client.Current, 1)), "friendr");
}
