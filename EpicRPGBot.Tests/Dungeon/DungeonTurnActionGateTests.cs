using EpicRPGBot.UI.Dungeon;
using Xunit;

namespace EpicRPGBot.Tests.Dungeon
{
    public sealed class DungeonTurnActionGateTests
    {
        [Fact]
        public void ShouldSendBite_AllowsOnlyOneActionDuringContinuousPlayerTurn()
        {
            var gate = new DungeonTurnActionGate();

            Assert.True(gate.ShouldSendBite(true));
            Assert.False(gate.ShouldSendBite(true));
            Assert.False(gate.ShouldSendBite(true));
        }

        [Fact]
        public void ShouldSendBite_RearmsAfterAnotherTurnBecomesVisible()
        {
            var gate = new DungeonTurnActionGate();

            Assert.True(gate.ShouldSendBite(true));
            Assert.False(gate.ShouldSendBite(false));
            Assert.True(gate.ShouldSendBite(true));
        }
    }
}
