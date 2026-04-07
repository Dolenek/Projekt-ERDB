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
    }
}
