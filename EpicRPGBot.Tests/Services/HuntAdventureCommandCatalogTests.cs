using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class HuntAdventureCommandCatalogTests
{
    [Theory]
    [InlineData(false, "rpg hunt", "rpg adv")]
    [InlineData(true, "rpg hunt h", "rpg adv h")]
    public void ResolvesCommandsForSelectedMode(
        bool useHardcore,
        string expectedHunt,
        string expectedAdventure)
    {
        Assert.Equal(expectedHunt, HuntAdventureCommandCatalog.ResolveHunt(useHardcore));
        Assert.Equal(expectedAdventure, HuntAdventureCommandCatalog.ResolveAdventure(useHardcore));
    }

    [Theory]
    [InlineData("rpg hunt", true, true)]
    [InlineData("rpg hunt h", true, true)]
    [InlineData("rpg adv", true, true)]
    [InlineData("RPG ADV H", true, true)]
    [InlineData("rpg work", true, false)]
    [InlineData("rpg hunt", false, false)]
    public void DeterminesWhetherHealShouldFollow(
        string command,
        bool healEnabled,
        bool expected)
    {
        Assert.Equal(expected, HuntAdventureCommandCatalog.ShouldSendHealAfter(command, healEnabled));
    }
}
