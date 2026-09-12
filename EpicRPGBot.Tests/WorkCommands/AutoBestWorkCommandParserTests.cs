using EpicRPGBot.UI.WorkCommands;
using Xunit;

namespace EpicRPGBot.Tests.WorkCommands;

public sealed class AutoBestWorkCommandParserTests
{
    [Fact]
    public void ProfileParser_ReadsLargeWalletBankAndTimeTravels()
    {
        const string message = @"firendr — profile
PROGRESS
Level: 261 (62.23%)
Area: 15 (Max: 15)
Time travels: 37
MONEY
🪙 Coins: 405,624,505,901
🏦 Bank:
524,088,362,400,869";

        var parsed = AutoBestProfileParser.TryParse(message, out var profile);

        Assert.True(parsed);
        Assert.Equal(37, profile.TimeTravels);
        Assert.Equal(405624505901m, profile.Coins);
        Assert.Equal(524088362400869m, profile.Bank);
        Assert.Equal(524493986906770m, profile.TotalCoins);
    }

    [Theory]
    [InlineData("Coins: 1\nTime travels: 2")]
    [InlineData("Bank: 1\nTime travels: 2")]
    [InlineData("Coins: 1\nBank: 2")]
    public void ProfileParser_RejectsMissingRequiredValues(string message)
    {
        Assert.False(AutoBestProfileParser.TryParse(message, out _));
    }

    [Fact]
    public void ProfessionParser_ReadsWorkerLevel()
    {
        const string message = "firendr — professions\n🧹 Worker Lv 104 | ■□□□□";

        Assert.True(WorkerProfessionParser.TryParseLevel(message, out var level));
        Assert.Equal(104, level);
    }

    [Fact]
    public void PotionParser_ReadsBothActivePotions()
    {
        const string message = @"firendr — boosts
These are your active boosts
Active items
• Fish potion: 0d 0h 29m 16s
• Wood potion: 0d 0h 29m 27s";

        var parsed = ActiveWorkPotionParser.TryParse(message, out var fish, out var wood);

        Assert.True(parsed);
        Assert.True(fish);
        Assert.True(wood);
    }

    [Theory]
    [InlineData("firendr — boosts\nActive items\nFish potion: 1h", true, false)]
    [InlineData("firendr — boosts\nYou don't have any active boosts", false, false)]
    public void PotionParser_HandlesOneOrNoPotion(string message, bool fish, bool wood)
    {
        Assert.True(ActiveWorkPotionParser.TryParse(message, out var parsedFish, out var parsedWood));
        Assert.Equal(fish, parsedFish);
        Assert.Equal(wood, parsedWood);
    }

    [Fact]
    public void PotionParser_RejectsUnrelatedReply()
    {
        Assert.False(ActiveWorkPotionParser.TryParse("firendr — profile", out _, out _));
    }
}
