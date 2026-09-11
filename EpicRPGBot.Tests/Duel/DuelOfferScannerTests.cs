using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelOfferScannerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task OffersAcrossChannels_AreReturnedInGlobalFifoOrder()
    {
        var client = CreateClient();
        client.MessagesByUrl["road50"] = new List<DiscordMessageSnapshot>
        {
            DuelTestMessages.Message("newer", "42 cf", Now.AddMinutes(-2), authorId: "202")
        };
        client.MessagesByUrl["road200"] = new List<DiscordMessageSnapshot>
        {
            DuelTestMessages.Message("older", "100 cf", Now.AddMinutes(-4), authorId: "201")
        };
        var scanner = CreateScanner(client);

        var offers = await scanner.ScanAsync(80, client.DiscoveredChannels.ToArray(), "100", "self", null, default);

        Assert.Equal(new[] { "older", "newer" }, offers.Select(offer => offer.Message.Id));
    }

    [Fact]
    public async Task Revalidate_DropsOfferThatGainedClaimReaction()
    {
        var client = CreateClient();
        var channel = client.DiscoveredChannels.Single(item => item.Name == "road-to-50");
        var original = DuelTestMessages.Message("offer", "42 cf", Now.AddMinutes(-2), authorId: "202");
        client.MessagesByUrl[channel.Url] = new List<DiscordMessageSnapshot>
        {
            DuelTestMessages.Message(
                "offer",
                "42 cf",
                Now.AddMinutes(-2),
                authorId: "202",
                reactions: new[] { new DiscordMessageReaction("✅", false, 1) })
        };
        var scanner = CreateScanner(client);

        var current = await scanner.RevalidateAsync(
            new DuelOffer(channel, original, 42),
            80,
            "100",
            "self",
            default);

        Assert.Null(current);
    }

    private static DuelOfferScanner CreateScanner(FakeDuelDiscordClient client)
    {
        return new DuelOfferScanner(client, new DuelChannelCatalog(), new DuelOfferParser(), () => Now);
    }

    private static FakeDuelDiscordClient CreateClient()
    {
        var client = new FakeDuelDiscordClient();
        client.DiscoveredChannels.Add(new DiscordChannelReference("1", "road-to-50", "road50"));
        client.DiscoveredChannels.Add(new DiscordChannelReference("2", "road-to-200", "road200"));
        return client;
    }
}
