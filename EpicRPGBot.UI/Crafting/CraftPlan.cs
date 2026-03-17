using System.Collections.Generic;

namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftPlan
    {
        public CraftPlan(IReadOnlyList<CraftPlanStep> steps)
        {
            Steps = steps;
        }

        public IReadOnlyList<CraftPlanStep> Steps { get; }
    }

    public sealed class CraftPlanStep
    {
        public CraftPlanStep(CraftItemDefinition definition, long amount)
        {
            Definition = definition;
            Amount = amount;
        }

        public CraftItemDefinition Definition { get; }
        public long Amount { get; }
    }
}
