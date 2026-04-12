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

            var notification = tracker.RegisterDetection("Captcha detected in latest message.");

            Assert.NotNull(notification);
            Assert.Equal(GuardAlertKind.FirstDetected, notification.Kind);
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
