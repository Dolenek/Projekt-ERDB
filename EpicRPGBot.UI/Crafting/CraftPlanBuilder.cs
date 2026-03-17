using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftPlanBuilder
    {
        private readonly CraftItemCatalog _catalog;

        public CraftPlanBuilder(CraftItemCatalog catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public CraftPlan Build(CraftRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var demand = new Dictionary<CraftItemKey, long>();
            foreach (var definition in _catalog.VisibleTargets)
            {
                var amount = request.GetAmount(definition.Key);
                if (amount > 0)
                {
                    demand[definition.Key] = amount;
                }
            }

            foreach (var definition in _catalog.PlanningOrder)
            {
                var amount = GetDemand(demand, definition.Key);
                if (amount <= 0 || !definition.IngredientKey.HasValue)
                {
                    continue;
                }

                checked
                {
                    demand[definition.IngredientKey.Value] = GetDemand(demand, definition.IngredientKey.Value) + (amount * definition.IngredientAmount);
                }
            }

            var steps = _catalog.VisibleTargets
                .Select(definition => new CraftPlanStep(definition, GetDemand(demand, definition.Key)))
                .Where(step => step.Amount > 0)
                .ToArray();

            return new CraftPlan(steps);
        }

        private static long GetDemand(IReadOnlyDictionary<CraftItemKey, long> demand, CraftItemKey key)
        {
            return demand.TryGetValue(key, out var amount) ? amount : 0;
        }
    }
}
