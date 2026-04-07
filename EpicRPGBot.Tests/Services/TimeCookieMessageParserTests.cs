using System;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class TimeCookieMessageParserTests
    {
        [Fact]
        public void TryParseReduction_TimeCookieReplyWithMinutesAhead_ReturnsReduction()
        {
            var parsed = TimeCookieMessageParser.TryParseReduction(
                "EPIC RPG\nYou ate a time cookie and jumped 30 minute(s) ahead.",
                out var reduction);

            Assert.True(parsed);
            Assert.Equal(TimeSpan.FromMinutes(30), reduction);
        }

        [Fact]
        public void TryParseReduction_MessageWithoutTimeCookie_ReturnsFalse()
        {
            var parsed = TimeCookieMessageParser.TryParseReduction(
                "EPIC RPG\nNothing happened.",
                out var reduction);

            Assert.False(parsed);
            Assert.Equal(TimeSpan.Zero, reduction);
        }
    }
}
