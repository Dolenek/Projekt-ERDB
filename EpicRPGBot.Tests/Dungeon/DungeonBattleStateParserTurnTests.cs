using EpicRPGBot.UI.Dungeon;
using EpicRPGBot.UI.Models;
using Xunit;

namespace EpicRPGBot.Tests.Dungeon
{
    public sealed class DungeonBattleStateParserTurnTests
    {
        private const string TurnText = "it's firendr's turn! What will you do, firendr?";

        [Fact]
        public void Parse_RequestsBiteWhenPlayerTurnActionIsAvailable()
        {
            var snapshot = new DiscordMessageSnapshot(
                "battle-1",
                TurnText,
                renderedText: TurnText,
                buttons: new[] { new DiscordMessageButton("BITE", 0, 0) });

            var state = new DungeonBattleStateParser().Parse(new[] { snapshot }, "firendr");

            Assert.True(state.ShouldBite);
        }

        [Fact]
        public void Parse_DoesNotRequestBiteAfterActionsBecomeUnavailable()
        {
            var snapshot = new DiscordMessageSnapshot(
                "battle-1",
                TurnText,
                renderedText: TurnText);

            var state = new DungeonBattleStateParser().Parse(new[] { snapshot }, "firendr");

            Assert.False(state.ShouldBite);
        }
    }
}
