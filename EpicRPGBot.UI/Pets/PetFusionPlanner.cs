using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Pets
{
    public sealed class PetFusionPlanner
    {
        public PetFusionStep Next(PetInventory inventory, PetFusionRequest request)
        {
            if (!inventory.Complete) return Stop("Refresh a complete pet inventory first.");
            var selected = inventory.Pets.Where(p => request.MaterialIds.Contains(p.Id)).ToArray();
            if (selected.Length != request.MaterialIds.Count) return Stop("Material selection is stale; refresh it.");
            if (request.Mode == PetFusionMode.Manual) return Manual(inventory, request, selected);
            if (!request.Options.TimeTravel.HasValue || request.Options.TimeTravel < 0) return Stop("Enter a valid TT value.");
            var available = selected.Where(p => PetProtection.Reason(p, inventory, request) == "")
                .OrderBy(p => p.Score).ThenBy(p => p.Id, StringComparer.Ordinal).ToList();
            return request.Mode == PetFusionMode.Upgrade ? Upgrade(inventory, request, available) : Reduce(inventory, request, available);
        }

        private static PetFusionStep Manual(PetInventory inventory, PetFusionRequest request, PetRecord[] selected)
        {
            if (selected.Length < 2) return Stop("Select at least two pets.");
            foreach (var pet in selected)
            {
                var reason = PetProtection.Reason(pet, inventory, request);
                if (reason != "") return Stop(pet.Id + ": " + reason);
            }
            return PetProtection.Compatible(selected) ? Step(selected, "Manual fusion") : Stop("Incompatible special species or skills.");
        }

        private static PetFusionStep Upgrade(PetInventory inventory, PetFusionRequest request, List<PetRecord> available)
        {
            var target = inventory.Pets.FirstOrDefault(p => p.Id == request.TargetId);
            if (target == null) return Stop("Select a target pet.");
            if (request.Goal < 2 || request.Goal > 25) return Stop("Target tier must be II–XXV.");
            if (target.Tier >= request.Goal) return Stop("Target tier reached.");
            var reason = PetProtection.Reason(target, inventory, request);
            if (reason != "") return Stop("Target " + target.Id + ": " + reason);
            var lower = PetFusionRecipes.LowerParent(target.Tier + 1, request.Options.TimeTravel.Value);
            if (lower == 0) return Stop("No TT recommendation for the next tier.");
            available.RemoveAll(p => p.Id == target.Id);
            var donor = available.FirstOrDefault(p => p.Tier == lower && AllowedPair(target, p, request.Options));
            if (donor != null) return Step(new[] { target, donor }, "Upgrade target " + target.Id);
            foreach (var species in available.Select(p => p.Species).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var candidate = new PetRecord { Species = species };
                if (!AllowedPair(target, candidate, request.Options)) continue;
                var preparation = Prepare(lower, species, available, request.Options.TimeTravel.Value);
                if (preparation != null && preparation.FirstStep != null) return preparation.FirstStep;
            }
            return Stop("No recommended combination or material chain is available.");
        }

        private static PetFusionStep Reduce(PetInventory inventory, PetFusionRequest request, List<PetRecord> available)
        {
            if (request.Goal < 1) return Stop("Remaining pet count must be at least one.");
            if (inventory.Pets.Count <= request.Goal) return Stop("Requested pet count reached.");
            for (var tier = 2; tier <= 20; tier++)
            {
                var lower = PetFusionRecipes.LowerParent(tier, request.Options.TimeTravel.Value);
                if (lower == 0) continue;
                foreach (var upper in available.Where(p => p.Tier == tier - 1))
                {
                    var donor = available.FirstOrDefault(p => p.Id != upper.Id && p.Tier == lower &&
                        AllowedReductionPair(upper, p, request.Options));
                    if (donor != null) return Step(new[] { upper, donor }, "Reduce count; recommended tier " + tier);
                }
            }
            return Stop("No recommended unprotected pair remains; count goal could not be reached.");
        }

        private static bool AllowedPair(PetRecord target, PetRecord donor, PetOptions options)
        {
            if (!options.MixSpecies && !string.Equals(target.Species, donor.Species, StringComparison.OrdinalIgnoreCase)) return false;
            var parents = new[] { target, donor };
            var results = PetProtection.ResultSpecies(parents);
            return PetProtection.Compatible(parents) && results.Count == 1 &&
                string.Equals(results[0], target.Species, StringComparison.OrdinalIgnoreCase);
        }

        private static bool AllowedReductionPair(PetRecord upper, PetRecord lower, PetOptions options) =>
            lower.IsSpecial ? AllowedPair(lower, upper, options) : AllowedPair(upper, lower, options);

        private static Preparation Prepare(int tier, string species, List<PetRecord> pool, int timeTravel)
        {
            var exact = pool.FirstOrDefault(p => p.Tier == tier && string.Equals(p.Species, species, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return new Preparation { Leaves = new[] { exact } };
            var lower = PetFusionRecipes.LowerParent(tier, timeTravel);
            if (lower == 0) return null;
            var high = Prepare(tier - 1, species, pool, timeTravel);
            if (high == null) return null;
            var remaining = pool.Except(high.Leaves).ToList();
            var low = Prepare(lower, species, remaining, timeTravel);
            if (low == null) return null;
            var leaves = high.Leaves.Concat(low.Leaves).ToArray();
            if (!PetProtection.Compatible(leaves)) return null;
            return new Preparation { Leaves = leaves, FirstStep = high.FirstStep ?? low.FirstStep ??
                Step(leaves, "Prepare tier " + tier + " material for target") };
        }

        private sealed class Preparation
        {
            public IReadOnlyList<PetRecord> Leaves { get; set; }
            public PetFusionStep FirstStep { get; set; }
        }

        private static PetFusionStep Step(IReadOnlyList<PetRecord> parents, string reason) =>
            new PetFusionStep { Parents = parents, Reason = reason };
        private static PetFusionStep Stop(string reason) => new PetFusionStep { Reason = reason };
    }
}
