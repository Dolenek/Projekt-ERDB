using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services;

public sealed class DiscordWebViewDemandTrackerTests
{
    [Fact]
    public void RemovingSelectionKeepsEngineDemandActive()
    {
        var tracker = new DiscordWebViewDemandTracker();
        tracker.Set(DiscordWebViewActivityReason.Selected, true);
        tracker.Set(DiscordWebViewActivityReason.Engine, true);

        tracker.Set(DiscordWebViewActivityReason.Selected, false);

        Assert.True(tracker.HasDemand);
        Assert.Equal(1, tracker.GetCount(DiscordWebViewActivityReason.Engine));
    }

    [Fact]
    public void RemovingSelectionKeepsPermanentDemandActive()
    {
        var tracker = new DiscordWebViewDemandTracker();
        tracker.Set(DiscordWebViewActivityReason.Permanent, true);
        tracker.Set(DiscordWebViewActivityReason.Selected, true);

        tracker.Set(DiscordWebViewActivityReason.Selected, false);

        Assert.True(tracker.HasDemand);
        Assert.Equal(1, tracker.GetCount(DiscordWebViewActivityReason.Permanent));
    }

    [Fact]
    public void WorkflowLeasesAreReferenceCounted()
    {
        var tracker = new DiscordWebViewDemandTracker();
        tracker.Add(DiscordWebViewActivityReason.Workflow);
        tracker.Add(DiscordWebViewActivityReason.Workflow);

        tracker.Remove(DiscordWebViewActivityReason.Workflow);

        Assert.True(tracker.HasDemand);
        Assert.Equal(1, tracker.GetCount(DiscordWebViewActivityReason.Workflow));
        tracker.Remove(DiscordWebViewActivityReason.Workflow);
        Assert.False(tracker.HasDemand);
    }
}
