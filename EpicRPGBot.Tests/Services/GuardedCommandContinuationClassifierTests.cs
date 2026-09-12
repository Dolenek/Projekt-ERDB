using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class GuardedCommandContinuationClassifierTests
    {
        [Fact]
        public void TrainingPromptAttachedToGuardClear_CompletesTrainingCommand()
        {
            var reply = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr earned 30,065 coins",
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr earned 30,065 coins\n" +
                "firendr is training in the river!\n" +
                "What is the name of this fish? You have 15 seconds!");

            var isContinuation = GuardedCommandContinuationClassifier.IsInlineContinuation(
                "rpg tr",
                reply);

            Assert.True(isContinuation);
        }

        [Fact]
        public void BareGuardClear_RequiresTrainingCommandRetry()
        {
            var reply = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr earned 30,065 coins");

            var isContinuation = GuardedCommandContinuationClassifier.IsInlineContinuation(
                "rpg tr",
                reply);

            Assert.False(isContinuation);
        }

        [Fact]
        public void TimeCookieReductionAttachedToGuardClear_CompletesCookieCommand()
        {
            var reply = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "You ate a time cookie and jumped 12 minute(s) ahead.");

            var isContinuation = GuardedCommandContinuationClassifier.IsInlineContinuation(
                "rpg use time cookie",
                reply);

            Assert.True(isContinuation);
        }

        [Fact]
        public void CooldownSnapshotAttachedToGuardClear_CompletesCooldownCommand()
        {
            var reply = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr's cooldowns\ntraining: 4m 20s");

            var isContinuation = GuardedCommandContinuationClassifier.IsInlineContinuation(
                "rpg cd",
                reply);

            Assert.True(isContinuation);
        }

        private static DiscordMessageSnapshot Snapshot(string text)
        {
            return new DiscordMessageSnapshot("clear-1", text, "EPIC RPG");
        }

        private static DiscordMessageSnapshot Snapshot(string text, string renderedText)
        {
            return new DiscordMessageSnapshot("clear-1", text, "EPIC RPG", renderedText);
        }
    }
}
