using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Pets
{
    public static class PetIdentityMapper
    {
        // IDs are display addresses, never persistent identity. Ambiguous groups fail closed.
        public static PetRecord Remap(PetInventory before, PetInventory after, PetFusionRequest request,
            IReadOnlyList<PetRecord> consumed = null)
        {
            if (!before.Complete || !after.Complete || before.Owner != after.Owner)
                throw new InvalidOperationException("Inventory owner changed or inventory is incomplete.");
            consumed = consumed ?? Array.Empty<PetRecord>();
            var survivors = before.Pets.Except(consumed).ToArray();
            if (after.Pets.Count != survivors.Length + (consumed.Count > 0 ? 1 : 0))
                throw new InvalidOperationException("Unexpected pet count; fusion outcome is unresolved.");
            var mappings = new Dictionary<string, string>();
            var remaining = after.Pets.ToList();
            foreach (var group in survivors.GroupBy(p => p.Fingerprint))
            {
                var matches = remaining.Where(p => p.Fingerprint == group.Key).ToArray();
                if (matches.Length != group.Count()) throw Ambiguous();
                var oldPets = group.ToArray();
                if (oldPets.Length > 1 && oldPets.Select(p => SelectionKey(p, request)).Distinct().Count() != 1)
                    throw Ambiguous();
                if (oldPets.Length > 1 && oldPets.Any(p => p.Id == request.TargetId)) throw Ambiguous();
                for (var index = 0; index < oldPets.Length; index++) mappings[oldPets[index].Id] = matches[index].Id;
                remaining.RemoveAll(p => p.Fingerprint == group.Key);
            }
            var result = consumed.Count > 0 ? ValidateResult(remaining, consumed) : null;
            request.LockedIds = MapSet(request.LockedIds, mappings);
            request.MaterialIds = MapSet(request.MaterialIds, mappings);
            var targetConsumed = consumed.Any(p => p.Id == request.TargetId);
            request.TargetId = targetConsumed ? result.Id : mappings.TryGetValue(request.TargetId, out var target) ? target : "";
            if (result != null && !targetConsumed) request.MaterialIds.Add(result.Id);
            return result;
        }

        private static PetRecord ValidateResult(List<PetRecord> remaining, IReadOnlyList<PetRecord> parents)
        {
            if (remaining.Count != 1) throw Ambiguous();
            var result = remaining[0];
            if (!result.Recognized || result.Tier < parents.Max(p => p.Tier) ||
                !PetProtection.ResultSpecies(parents).Contains(result.Species, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException("Inventory change does not match the requested fusion.");
            return result;
        }

        private static string SelectionKey(PetRecord pet, PetFusionRequest request) =>
            request.LockedIds.Contains(pet.Id) + ":" + request.MaterialIds.Contains(pet.Id);
        private static HashSet<string> MapSet(HashSet<string> ids, Dictionary<string, string> mappings) =>
            new HashSet<string>(ids.Where(mappings.ContainsKey).Select(id => mappings[id]));
        private static InvalidOperationException Ambiguous() => new InvalidOperationException(
            "Pet identities are ambiguous. Refresh and explicitly reset/reselect locks and material before continuing.");
    }
}
