using System.Collections.Generic;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class ConfiguredWorkCommandCatalogTests
    {
        [Fact]
        public void BuildCooldownAliases_IncludesConfiguredAndLegacyAliases()
        {
            var selections = AreaWorkCommandSettings.CreateDefaultSelections();
            selections[7] = "rpg dynamite";
            selections[12] = "rpg bigboat";

            var aliases = ConfiguredWorkCommandCatalog.BuildCooldownAliases(
                AreaWorkCommandSettings.Serialize(selections));

            Assert.Contains("dynamite", aliases);
            Assert.Contains("bigboat", aliases);
            Assert.Contains("fish", aliases);
        }

        [Fact]
        public void IsWorkCommand_CustomConfiguredCommand_ReturnsTrue()
        {
            var selections = new Dictionary<int, string> { [1] = "rpg dynamite" };
            for (var area = AreaWorkCommandSettings.MinimumArea + 1; area <= AreaWorkCommandSettings.MaximumArea; area++)
            {
                selections[area] = "rpg chop";
            }

            var isWorkCommand = ConfiguredWorkCommandCatalog.IsWorkCommand(
                "rpg dynamite",
                AreaWorkCommandSettings.Serialize(selections));

            Assert.True(isWorkCommand);
        }

        [Fact]
        public void IsWorkCommand_DefaultAndLegacyCommandsStayRecognized()
        {
            var defaults = AreaWorkCommandSettings.DefaultSerializedMap;

            Assert.True(ConfiguredWorkCommandCatalog.IsWorkCommand("rpg chainsaw", defaults));
            Assert.True(ConfiguredWorkCommandCatalog.IsWorkCommand("rpg fish", defaults));
        }
    }
}
