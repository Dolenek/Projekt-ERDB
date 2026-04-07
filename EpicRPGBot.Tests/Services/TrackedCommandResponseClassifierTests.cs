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
                "EPIC RPG\nfirendr found and killed a GIANT SCORPION\nBy the way, did you try the new event? --> rpg easter",
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
    }
}
