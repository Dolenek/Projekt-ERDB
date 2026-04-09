using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class TrackedCommandResponseClassifierTests
    {
        [Fact]
        public void HuntReplyWithEmbeddedCommandHint_IsStillTracked()
        {
            var snapshot = new DiscordMessageSnapshot(
                "m1",
                "EPIC RPG\ntestplayer found and killed a GIANT SCORPION\nBy the way, did you try the new event? --> rpg easter",
                "EPIC RPG");

            Assert.True(TrackedCommandResponseClassifier.LooksLikeTrackedCommandResponse(snapshot));
            Assert.True(TrackedCommandResponseClassifier.TryInferKind(snapshot.Text, out var kind));
            Assert.Equal(TrackedCommandKind.Hunt, kind);
        }

        [Fact]
        public void PureCommandHintMessage_IsNotTracked()
        {
            var snapshot = new DiscordMessageSnapshot(
                "m2",
                "EPIC RPG\nBy the way, did you try the new event? --> rpg easter",
                "EPIC RPG");

            Assert.False(TrackedCommandResponseClassifier.LooksLikeTrackedCommandResponse(snapshot));
        }

        [Fact]
        public void DailyReply_IsTrackedAsDaily()
        {
            var snapshot = new DiscordMessageSnapshot(
                "m3",
                "EPIC RPG\nYou claimed your daily rewards successfully.",
                "EPIC RPG");

            Assert.True(TrackedCommandResponseClassifier.LooksLikeTrackedCommandResponse(snapshot));
            Assert.True(TrackedCommandResponseClassifier.TryInferKind(snapshot.Text, out var kind));
            Assert.Equal(TrackedCommandKind.Daily, kind);
        }

        [Fact]
        public void WeeklyReply_IsTrackedAsWeekly()
        {
            var snapshot = new DiscordMessageSnapshot(
                "m4",
                "EPIC RPG\nYou claimed your weekly rewards successfully.",
                "EPIC RPG");

            Assert.True(TrackedCommandResponseClassifier.LooksLikeTrackedCommandResponse(snapshot));
            Assert.True(TrackedCommandResponseClassifier.TryInferKind(snapshot.Text, out var kind));
            Assert.Equal(TrackedCommandKind.Weekly, kind);
        }

        [Fact]
        public void PreviousCommandBusyReply_IsDetected()
        {
            var snapshot = new DiscordMessageSnapshot(
                "m5",
                "@Firender, you can't do this! end your previous command",
                "EPIC RPG");

            Assert.True(TrackedCommandResponseClassifier.IsPreviousCommandBusyReply(snapshot));
        }

        [Fact]
        public void PreviousCommandBusyReply_IsNotTrackedAsCooldownResponse()
        {
            var snapshot = new DiscordMessageSnapshot(
                "m6",
                "@Firender, you can't do this! end your previous command",
                "EPIC RPG");

            Assert.False(TrackedCommandResponseClassifier.LooksLikeTrackedCommandResponse(snapshot));
        }
    }
}
