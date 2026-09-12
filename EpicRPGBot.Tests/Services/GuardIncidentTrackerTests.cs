using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class GuardIncidentTrackerTests
    {
        [Fact]
        public void RegisterDetection_ActivatesIncidentUntilClear()
        {
            var tracker = new GuardIncidentTracker();
            var reference = new DiscordMessageReference(
                "chat-messages-333333333333333",
                DiscordTabRole.Bot,
                "https://discord.com/channels/111111111111111/222222222222222");

            var notification = tracker.RegisterDetection("Puzzle detected in latest message.", reference);

            Assert.NotNull(notification);
            Assert.Equal(GuardAlertKind.FirstDetected, notification.Kind);
            Assert.Same(reference, notification.MessageReference);
            Assert.True(tracker.IsActive);

            var cleared = tracker.ClearIfActive();

            Assert.NotNull(cleared);
            Assert.Equal(GuardAlertKind.Cleared, cleared.Kind);
            Assert.False(tracker.IsActive);
        }

        [Fact]
        public void ContainsGuardClear_RequiresEpicGuardClearPhrase()
        {
            Assert.False(GuardIncidentTracker.ContainsGuardClear("Everything seems fine, keep playing."));
            Assert.False(GuardIncidentTracker.ContainsGuardClear("EPIC GUARD: Everything seems fine."));
            Assert.False(GuardIncidentTracker.ContainsGuardClear("keep playing"));
            Assert.True(GuardIncidentTracker.ContainsGuardClear("EPIC GUARD: Everything seems fine now, keep playing"));
        }
    }
}
