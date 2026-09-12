using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Pets
{
    public static class PetProtection
    {
        public static string Reason(PetRecord pet, PetInventory inventory, PetFusionRequest request)
        {
            var options = request.Options;
            if (!inventory.Complete) return "Incomplete inventory";
            if (!pet.Recognized) return "Unrecognized pet details";
            if (!string.Equals(pet.Status, "idle", StringComparison.OrdinalIgnoreCase)) return "Pet is not idle";
            if (request.LockedIds.Contains(pet.Id)) return "Locked";
            if (options.ProtectSpecial && pet.IsSpecial) return "Special species protected";
            if (options.ProtectEpic && pet.HasSkill("EPIC")) return "EPIC protected";
            if (options.ProtectAscended && pet.HasSkill("ASCENDED")) return "ASCENDED protected";
            if (options.ProtectPerfect && pet.HasSkill("PERFECT")) return "PERFECT protected";
            var best = inventory.Pets.Where(p => string.Equals(p.Species, pet.Species, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Tier).ThenByDescending(p => p.Score).ThenBy(p => p.Id, StringComparer.Ordinal).First();
            return options.KeepBest && best.Id == pet.Id ? "Best of species protected" : "";
        }

        public static IReadOnlyList<string> ResultSpecies(IEnumerable<PetRecord> parents)
        {
            var pets = parents.ToArray();
            var special = pets.Where(p => p.IsSpecial).Select(p => p.Species).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (special.Length > 0) return special;
            var groups = pets.GroupBy(p => p.Species, StringComparer.OrdinalIgnoreCase).ToArray();
            return groups.Length == 0 ? Array.Empty<string>() : groups.Where(g => g.Count() == groups.Max(x => x.Count()))
                .Select(g => g.Key).ToArray();
        }

        public static bool Compatible(IEnumerable<PetRecord> parents)
        {
            var pets = parents.ToArray();
            return pets.Select(p => p.Species).Where(s => pets.Any(p => p.Species == s && p.IsSpecial))
                .Distinct(StringComparer.OrdinalIgnoreCase).Count() <= 1 &&
                pets.SelectMany(p => SpecialSkills(p.Skills)).Distinct(StringComparer.OrdinalIgnoreCase).Count() <= 1;
        }

        private static IEnumerable<string> SpecialSkills(string skills)
        {
            var ordinary = new[] { "FAST", "HAPPY", "CLEVER", "DIGGER", "LUCKY", "TIME TRAVELER",
                "EPIC", "ASCENDED", "PERFECT", "FIGHTER", "MASTER", "NORMIE" };
            var remaining = System.Text.RegularExpressions.Regex.Replace(skills, @"\[[^\]]*\]", "");
            foreach (var name in ordinary.OrderByDescending(s => s.Length))
                remaining = System.Text.RegularExpressions.Regex.Replace(remaining, @"\b" + name + @"\b", "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return remaining.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim()).Where(s => s.Length > 0);
        }
    }
}
