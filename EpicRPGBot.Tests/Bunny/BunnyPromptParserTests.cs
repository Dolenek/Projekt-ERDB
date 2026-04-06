using EpicRPGBot.UI.Bunny;
using Xunit;

namespace EpicRPGBot.Tests.Bunny
{
    public sealed class BunnyPromptParserTests
    {
        private readonly BunnyPromptParser _parser = new BunnyPromptParser();

        [Fact]
        public void Parse_ReturnsStats_ForValidPrompt()
        {
            var result = _parser.Parse(
                ":heart: Happiness: 48\n:carrot: Hunger: 38\nHow to get a bunny\nSee a guide how to get bunnies with rpg egg info bunny");

            Assert.True(result.IsBunnyPrompt);
            Assert.True(result.HasReadableStats);
            Assert.Equal(48, result.Happiness);
            Assert.Equal(38, result.Hunger);
        }

        [Fact]
        public void Parse_RejectsMessagesWithoutFooter()
        {
            var result = _parser.Parse(":heart: Happiness: 48\n:carrot: Hunger: 38");

            Assert.False(result.IsBunnyPrompt);
            Assert.False(result.HasReadableStats);
        }

        [Fact]
        public void Parse_RecognizesPromptWithoutReadableStats()
        {
            var result = _parser.Parse(
                ":heart: Happiness:\nHow to get a bunny\nSee a guide how to get bunnies with rpg egg info bunny");

            Assert.True(result.IsBunnyPrompt);
            Assert.False(result.HasReadableStats);
        }
    }
}
