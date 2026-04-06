using EpicRPGBot.UI.Bunny;
using Xunit;

namespace EpicRPGBot.Tests.Bunny
{
    public sealed class BunnyCatchPlanBuilderTests
    {
        private readonly BunnyCatchPlanBuilder _builder = new BunnyCatchPlanBuilder();

        [Fact]
        public void Build_UsesExpectedSampleReply()
        {
            var plan = _builder.Build(new BunnyPromptParseResult(true, true, 48, 38, string.Empty));

            Assert.Equal("feed feed pat pat pat pat", plan.ReplyText);
            Assert.False(plan.UsedFallback);
        }

        [Fact]
        public void Build_PrefersShortestGuaranteedPlan()
        {
            var plan = _builder.Build(new BunnyPromptParseResult(true, true, 70, 0, string.Empty));

            Assert.Equal("pat pat", plan.ReplyText);
        }

        [Fact]
        public void Build_MaximizesEggBonusByChoosingFewestGuaranteedActions()
        {
            var plan = _builder.Build(new BunnyPromptParseResult(true, true, 77, 0, string.Empty));

            Assert.Equal("pat", plan.ReplyText);
            Assert.False(plan.UsedFallback);
        }

        [Fact]
        public void Build_StillSendsOneActionWhenCatchIsAlreadyGuaranteed()
        {
            var plan = _builder.Build(new BunnyPromptParseResult(true, true, 90, 0, string.Empty));

            Assert.Equal("pat", plan.ReplyText);
        }

        [Fact]
        public void Build_UsesBestSixActionHeuristicWhenNoGuaranteedCatchExists()
        {
            var plan = _builder.Build(new BunnyPromptParseResult(true, true, 0, 100, string.Empty));

            Assert.Equal("feed feed feed feed feed pat", plan.ReplyText);
        }

        [Fact]
        public void Build_UsesFallbackWhenStatsAreUnreadable()
        {
            var plan = _builder.Build(new BunnyPromptParseResult(true, false, 0, 0, string.Empty));

            Assert.Equal(BunnyCatchPlanBuilder.FallbackReplyText, plan.ReplyText);
            Assert.True(plan.UsedFallback);
        }
    }
}
