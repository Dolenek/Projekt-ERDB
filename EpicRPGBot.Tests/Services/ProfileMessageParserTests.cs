using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class ProfileMessageParserTests
    {
        [Fact]
        public void TryParsePlayerName_ParsesProfileHeader()
        {
            const string message = @"testplayer — profile
this is the best title
PROGRESS
Area: 7 (Max: 7)";

            var parsed = ProfileMessageParser.TryParsePlayerName(message, out var playerName);

            Assert.True(parsed);
            Assert.Equal("testplayer", playerName);
        }

        [Fact]
        public void TryParseLevel_ParsesProfileProgress()
        {
            const string message = "firendr — profile\nPROGRESS\nLevel: 142 (70.12%)";

            Assert.True(ProfileMessageParser.TryParseLevel(message, out var level));
            Assert.Equal(142, level);
        }

        [Theory]
        [InlineData("firendr — profile\nPROGRESS")]
        [InlineData("Level: nope")]
        [InlineData("Level: 0")]
        public void TryParseLevel_RejectsMissingOrInvalidLevel(string message)
        {
            Assert.False(ProfileMessageParser.TryParseLevel(message, out _));
        }
    }
}
