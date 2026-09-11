using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class DiscordCommandSendPolicyTests
    {
        [Theory]
        [InlineData("rpg dung <@123456>")]
        [InlineData("rpg dung @so.over.it")]
        [InlineData("  RPG   DUNG   @partner  ")]
        public void DungeonEntry_UsesStableRenderedTextToken(string command)
        {
            Assert.Equal("rpg dung", DiscordCommandSendPolicy.GetOutgoingDetectionToken(command));
        }

        [Fact]
        public void DungeonEntry_DisablesBlindResend()
        {
            Assert.False(DiscordCommandSendPolicy.AllowsBlindResend("rpg dung <@123456>"));
        }

        [Fact]
        public void OrdinaryCommand_PreservesNormalizedTextAndRetry()
        {
            Assert.Equal("rpg trade a all", DiscordCommandSendPolicy.GetOutgoingDetectionToken("rpg  trade a all"));
            Assert.True(DiscordCommandSendPolicy.AllowsBlindResend("rpg trade a all"));
        }
    }
}
