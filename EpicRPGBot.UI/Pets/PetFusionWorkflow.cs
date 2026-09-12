using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Pets
{
    public sealed class PetFusionWorkflow
    {
        private readonly IPetGateway _gateway;
        private readonly PetFusionPlanner _planner;
        private int _running;
        public PetFusionWorkflow(IPetGateway gateway, PetFusionPlanner planner)
        {
            _gateway = gateway;
            _planner = planner;
        }

        public async Task<PetInventory> RunAsync(PetInventory inventory, PetFusionRequest request,
            Action<PetInventory> onInventory, Action<string> report, CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
                throw new InvalidOperationException("A pet fusion run is already active.");
            try { return await RunCoreAsync(inventory, request, onInventory, report, cancellationToken); }
            finally { Interlocked.Exchange(ref _running, 0); }
        }

        private async Task<PetInventory> RunCoreAsync(PetInventory inventory, PetFusionRequest request,
            Action<PetInventory> onInventory, Action<string> report, CancellationToken cancellationToken)
        {
            var refreshed = await _gateway.LoadAsync(cancellationToken);
            PetIdentityMapper.Remap(inventory, refreshed, request);
            inventory = refreshed;
            onInventory(inventory);
            while (!cancellationToken.IsCancellationRequested)
            {
                var step = _planner.Next(inventory, request);
                report(step.Reason);
                if (!step.Available) break;
                cancellationToken.ThrowIfCancellationRequested();
                report(step.Command);
                // Stop is a boundary request. Never cancel observation of an already sent fusion.
                await _gateway.FuseAsync(step);
                refreshed = await _gateway.LoadAsync(CancellationToken.None);
                var result = PetIdentityMapper.Remap(inventory, refreshed, request, step.Parents);
                inventory = refreshed;
                onInventory(inventory);
                report("Result: " + result.Id + " " + result.Species + " tier " + result.Tier);
                if (request.Mode == PetFusionMode.Manual) break;
            }
            report(cancellationToken.IsCancellationRequested ? "Stopped after the current fusion." : "Fusion run finished.");
            return inventory;
        }
    }
}
