using EpicRPGBot.UI.Pets;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetWorkflowTests
{
    [Fact]
    public async Task ManualRunRefreshesBeforeAndAfterFusion()
    {
        var before = Inventory(Pet("A"), Pet("B"));
        var gateway = new Gateway(before, Inventory(Pet("Z", 2)));
        var result = await Run(gateway, before, Request(PetFusionMode.Manual, "A", "B"));
        Assert.Equal(2, gateway.Loads);
        Assert.Equal(1, gateway.Fusions);
        Assert.Equal("Z", Assert.Single(result.Pets).Id);
    }

    [Fact]
    public async Task MissingFusionReplyNeverRetriesOrSendsAnotherInventoryCommand()
    {
        var before = Inventory(Pet("A"), Pet("B"));
        var gateway = new Gateway(before) { FusionFailure = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Run(gateway, before, Request(PetFusionMode.Manual, "A", "B")));
        Assert.Equal(1, gateway.Fusions);
        Assert.Equal(1, gateway.Loads);
    }

    [Fact]
    public async Task StopDuringFusionStillReconcilesButDoesNotSendNextFusion()
    {
        using var cancellation = new CancellationTokenSource();
        var before = Inventory(Pet("A", score: 1), Pet("B", score: 2), Pet("C", score: 3));
        var gateway = new Gateway(before, Inventory(Pet("A", score: 3), Pet("B", 2))) { OnFusion = cancellation.Cancel };
        var request = Request(PetFusionMode.Reduce, "A", "B", "C");
        request.Goal = 1;
        await Run(gateway, before, request, cancellation.Token);
        Assert.Equal(1, gateway.Fusions);
        Assert.Equal(2, gateway.Loads);
        Assert.False(gateway.LastLoadToken.CanBeCanceled);
    }

    [Fact]
    public async Task TierFailureReplansUsingActualResult()
    {
        var before = Inventory(Pet("A", 2), Pet("B", score: 1), Pet("C", score: 2));
        var gateway = new Gateway(before, Inventory(Pet("X", 2), Pet("Y", score: 2)), Inventory(Pet("Z", 3)));
        var request = Request(PetFusionMode.Upgrade, "B", "C");
        request.TargetId = "A";
        request.Goal = 3;
        await Run(gateway, before, request);
        Assert.Equal(2, gateway.Fusions);
        Assert.Equal("Z", request.TargetId);
    }

    [Fact]
    public async Task AmbiguousPostFusionInventoryStopsBeforeNextSend()
    {
        var before = Inventory(Pet("A", 2), Pet("B"), Pet("C", 3));
        var gateway = new Gateway(before, Inventory(Pet("X", 3), Pet("Y", 3)));
        var request = Request(PetFusionMode.Upgrade, "B");
        request.TargetId = "A";
        request.Goal = 4;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Run(gateway, before, request));
        Assert.Equal(1, gateway.Fusions);
    }

    [Fact]
    public async Task ConcurrentRunIsRejected()
    {
        var before = Inventory(Pet("A"), Pet("B"));
        var release = new TaskCompletionSource<bool>();
        var gateway = new Gateway(before, Inventory(Pet("Z", 2))) { FusionWait = release.Task };
        var workflow = new PetFusionWorkflow(gateway, new PetFusionPlanner());
        var first = workflow.RunAsync(before, Request(PetFusionMode.Manual, "A", "B"), _ => { }, _ => { }, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.RunAsync(before,
            Request(PetFusionMode.Manual, "A", "B"), _ => { }, _ => { }, default));
        release.SetResult(true);
        await first;
        Assert.Equal(1, gateway.Fusions);
    }

    private static Task<PetInventory> Run(Gateway gateway, PetInventory before, PetFusionRequest request, CancellationToken token = default) =>
        new PetFusionWorkflow(gateway, new PetFusionPlanner()).RunAsync(before, request, _ => { }, _ => { }, token);

    private sealed class Gateway(params PetInventory[] inventories) : IPetGateway
    {
        private readonly Queue<PetInventory> _inventories = new(inventories);
        public int Loads { get; private set; }
        public int Fusions { get; private set; }
        public bool FusionFailure { get; init; }
        public Action? OnFusion { get; init; }
        public Task FusionWait { get; init; } = Task.CompletedTask;
        public CancellationToken LastLoadToken { get; private set; }
        public Task<PetInventory> LoadAsync(CancellationToken token)
        {
            LastLoadToken = token;
            Loads++;
            return Task.FromResult(_inventories.Dequeue());
        }
        public async Task FuseAsync(PetFusionStep step)
        {
            Fusions++;
            if (FusionFailure) throw new InvalidOperationException("Reply missing");
            OnFusion?.Invoke();
            await FusionWait;
        }
    }
}
