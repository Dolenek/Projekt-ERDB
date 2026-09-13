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
        public void BareGuardClear_RequiresWaitingForEditedResult()
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
        public void FarmResultAttachedToGuardClear_CompletesOriginalFarmCommand()
        {
            var reply = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr earned 27,164 coins\n" +
                "firendr plants seed in the ground...\n" +
                "55 carrot have grown from the seed\nEarned 138,226 XP");

            var isContinuation = GuardedCommandContinuationClassifier.IsInlineContinuation(
                "rpg farm",
                reply);

            Assert.True(isContinuation);
        }

        [Fact]
        public void FindContinuation_ReturnsEditedClearMessage()
        {
            var initialClear = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr earned 27,164 coins");
            var editedClear = Snapshot(
                initialClear.Text + "\n" +
                "firendr plants seed in the ground...\n" +
                "55 carrot have grown from the seed");

            var continuation = GuardedCommandContinuationClassifier.FindContinuation(
                "rpg farm",
                initialClear.Id,
                new[] { editedClear });

            Assert.Same(editedClear, continuation);
        }

        [Theory]
        [InlineData(
            "rpg chainsaw",
            "firendr is chopping with THREE CHAINSAW!!\nWOO! firendr got 78 SUPER log")]
        [InlineData(
            "rpg tr",
            "firendr is training in the river!\nWhat is the name of this fish? You have 15 seconds!")]
        [InlineData(
            "rpg use time cookie",
            "You ate a time cookie and jumped 12 minute(s) ahead.")]
        public void FindContinuation_ReturnsSeparateGroupedResultWithoutRepeatedAuthor(
            string command,
            string resultText)
        {
            var clear = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr earned 26,775 coins");
            var commandResult = new DiscordMessageSnapshot("result-1", resultText);

            var continuation = GuardedCommandContinuationClassifier.FindContinuation(
                command,
                clear.Id,
                new[] { clear, commandResult });

            Assert.Same(commandResult, continuation);
        }

        [Fact]
        public void FindContinuation_DoesNotTreatAnotherUsersMessageAsGroupedEpicResult()
        {
            var clear = Snapshot(
                "EPIC GUARD: Everything seems fine firendr, keep playing");
            var playerMessage = new DiscordMessageSnapshot(
                "player-1",
                "firendr is chopping with THREE CHAINSAW!!",
                "Firender");

            var continuation = GuardedCommandContinuationClassifier.FindContinuation(
                "rpg chainsaw",
                clear.Id,
                new[] { clear, playerMessage });

            Assert.Null(continuation);
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
