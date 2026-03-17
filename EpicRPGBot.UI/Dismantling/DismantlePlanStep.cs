using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI.Dismantling
{
    public sealed class DismantlePlanStep
    {
        public DismantlePlanStep(CraftItemDefinition definition, string commandAmount, long producedAmount = 0)
        {
            Definition = definition;
            CommandAmount = commandAmount;
            ProducedAmount = producedAmount;
        }

        public CraftItemDefinition Definition { get; }
        public string CommandAmount { get; }
        public long ProducedAmount { get; }
    }
}
