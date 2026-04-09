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

        [Fact]
        public void Parse_ReturnsStats_ForCatPetPrompt()
        {
            var result = _parser.Parse(
                "SUDDENLY, A CAT TIER IS APPROACHING friendr\nHappiness: 64\nHunger: 50\nUse \"info\" to get information about pets");

            Assert.True(result.IsBunnyPrompt);
            Assert.True(result.HasReadableStats);
            Assert.Equal(64, result.Happiness);
            Assert.Equal(50, result.Hunger);
        }

        [Fact]
        public void Parse_ReturnsStats_ForDragonPetPrompt()
        {
            var result = _parser.Parse(
                "SUDDENLY, A DRAGON TIER IS APPROACHING friendr\nHappiness: 34\nHunger: 53\nUse \"info\" to get information about pets");

            Assert.True(result.IsBunnyPrompt);
            Assert.True(result.HasReadableStats);
            Assert.Equal(34, result.Happiness);
            Assert.Equal(53, result.Hunger);
        }

        [Fact]
        public void Parse_ReturnsStats_ForDragonTierTwoPetPrompt()
        {
            var result = _parser.Parse(
                "SUDDENLY, A DRAGON TIER II IS APPROACHING friendr\nHappiness: 80\nHunger: 0\nUse \"info\" to get information about pets");

            Assert.True(result.IsBunnyPrompt);
            Assert.True(result.HasReadableStats);
            Assert.Equal(80, result.Happiness);
            Assert.Equal(0, result.Hunger);
        }

        [Fact]
        public void Parse_RejectsPetMessagesWithoutPetFooter()
        {
            var result = _parser.Parse(
                "SUDDENLY, A DOG TIER IS APPROACHING friendr\nHappiness: 40\nHunger: 30");

            Assert.False(result.IsBunnyPrompt);
            Assert.False(result.HasReadableStats);
        }
    }
}
