using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Pets
{
    public sealed class PetRecord
    {
        public string Id { get; set; } = "";
        public string Species { get; set; } = "";
        public int Tier { get; set; }
        public int Score { get; set; }
        public string Status { get; set; } = "";
        public string Skills { get; set; } = "";
        public bool Recognized { get; set; }
        public bool IsSpecial => !new[] { "cat", "dog", "dragon" }.Contains(Species.ToLowerInvariant());
        public string Fingerprint => string.Join("|", Species.ToLowerInvariant(), Tier, Score,
            Skills.ToLowerInvariant());
        public bool HasSkill(string skill) => System.Text.RegularExpressions.Regex.IsMatch(Skills,
            @"(?<![a-z])" + System.Text.RegularExpressions.Regex.Escape(skill) + @"(?![a-z])",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    public sealed class PetInventory
    {
        public string Owner { get; set; } = "";
        public IReadOnlyList<PetRecord> Pets { get; set; } = Array.Empty<PetRecord>();
        public bool Complete { get; set; }
    }

    public sealed class PetOptions
    {
        public int? TimeTravel { get; set; }
        public bool KeepBest { get; set; } = true;
        public bool ProtectSpecial { get; set; } = true;
        public bool ProtectEpic { get; set; } = true;
        public bool ProtectAscended { get; set; } = true;
        public bool ProtectPerfect { get; set; } = true;
        public bool MixSpecies { get; set; }
    }

    public enum PetFusionMode { Manual, Upgrade, Reduce }

    public sealed class PetFusionRequest
    {
        public PetFusionMode Mode { get; set; }
        public PetOptions Options { get; set; } = new PetOptions();
        public HashSet<string> MaterialIds { get; set; } = new HashSet<string>();
        public HashSet<string> LockedIds { get; set; } = new HashSet<string>();
        public string TargetId { get; set; } = "";
        public int Goal { get; set; }
    }

    public sealed class PetFusionStep
    {
        public IReadOnlyList<PetRecord> Parents { get; set; } = Array.Empty<PetRecord>();
        public string Reason { get; set; } = "";
        public bool Available => Parents.Count >= 2;
        public string Command
        {
            get
            {
                if (Parents.Count < 2 || Parents.Any(p => p == null ||
                    !System.Text.RegularExpressions.Regex.IsMatch(p.Id ?? "", @"^[A-Za-z0-9]+$")) ||
                    Parents.Select(p => p.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Parents.Count)
                    throw new InvalidOperationException("Fusion requires at least two distinct, valid pet IDs.");
                return "rpg pets fusion " + string.Join(" ", Parents.Select(p => p.Id));
            }
        }
    }
}
