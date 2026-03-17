using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftItemCatalog
    {
        private readonly IReadOnlyDictionary<CraftItemKey, CraftItemDefinition> _definitions;
        private readonly IReadOnlyDictionary<string, CraftItemDefinition> _definitionsByName;

        public CraftItemCatalog(IEnumerable<CraftItemDefinition> definitions)
        {
            var definitionList = definitions?.ToArray() ?? Array.Empty<CraftItemDefinition>();
            AllItems = definitionList;
            _definitions = definitionList.ToDictionary(item => item.Key);
            _definitionsByName = definitionList.ToDictionary(item => item.DisplayName, StringComparer.OrdinalIgnoreCase);
            VisibleTargets = definitionList
                .Where(item => item.IsCraftable)
                .OrderBy(item => item.Rank)
                .ToArray();
            PlanningOrder = VisibleTargets
                .OrderByDescending(item => item.Rank)
                .ToArray();
        }

        public static CraftItemCatalog Default { get; } = new CraftItemCatalog(new[]
        {
            new CraftItemDefinition(CraftItemKey.WoodenLog, "wooden log", "wooden log", 0, null, 0),
            new CraftItemDefinition(CraftItemKey.EpicLog, "epic log", "epic log", 1, CraftItemKey.WoodenLog, 25, CraftItemKey.WoodenLog, 20),
            new CraftItemDefinition(CraftItemKey.SuperLog, "super log", "super log", 2, CraftItemKey.EpicLog, 10, CraftItemKey.EpicLog, 8),
            new CraftItemDefinition(CraftItemKey.MegaLog, "mega log", "mega log", 3, CraftItemKey.SuperLog, 10, CraftItemKey.SuperLog, 8),
            new CraftItemDefinition(CraftItemKey.HyperLog, "hyper log", "hyper log", 4, CraftItemKey.MegaLog, 10, CraftItemKey.MegaLog, 8),
            new CraftItemDefinition(CraftItemKey.UltraLog, "ultra log", "ultra log", 5, CraftItemKey.HyperLog, 10, CraftItemKey.HyperLog, 8),
            new CraftItemDefinition(CraftItemKey.Apple, "apple", "apple", 0, null, 0),
            new CraftItemDefinition(CraftItemKey.Banana, "banana", "banana", 1, CraftItemKey.Apple, 15, CraftItemKey.Apple, 12),
            new CraftItemDefinition(CraftItemKey.NormieFish, "normie fish", "normie fish", 0, null, 0),
            new CraftItemDefinition(CraftItemKey.GoldenFish, "golden fish", "golden fish", 1, CraftItemKey.NormieFish, 15, CraftItemKey.NormieFish, 12),
            new CraftItemDefinition(CraftItemKey.EpicFish, "epic fish", "epic fish", 2, CraftItemKey.GoldenFish, 100, CraftItemKey.GoldenFish, 80)
        });

        public static CraftItemCatalog LogChain => Default;

        public IReadOnlyList<CraftItemDefinition> AllItems { get; }
        public IReadOnlyList<CraftItemDefinition> VisibleTargets { get; }
        public IReadOnlyList<CraftItemDefinition> PlanningOrder { get; }

        public CraftItemDefinition Get(CraftItemKey key)
        {
            return _definitions[key];
        }

        public bool TryGet(CraftItemKey key, out CraftItemDefinition definition)
        {
            return _definitions.TryGetValue(key, out definition);
        }

        public bool TryGetByDisplayName(string displayName, out CraftItemDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                definition = null;
                return false;
            }

            return _definitionsByName.TryGetValue(displayName.Trim(), out definition);
        }
    }
}
