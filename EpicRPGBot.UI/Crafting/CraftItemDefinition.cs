namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftItemDefinition
    {
        public CraftItemDefinition(
            CraftItemKey key,
            string displayName,
            string commandName,
            int rank,
            CraftItemKey? ingredientKey,
            long ingredientAmount,
            CraftItemKey? dismantleOutputKey = null,
            long dismantleOutputAmount = 0)
        {
            Key = key;
            DisplayName = displayName;
            CommandName = commandName;
            Rank = rank;
            IngredientKey = ingredientKey;
            IngredientAmount = ingredientAmount;
            DismantleOutputKey = dismantleOutputKey;
            DismantleOutputAmount = dismantleOutputAmount;
        }

        public CraftItemKey Key { get; }
        public string DisplayName { get; }
        public string CommandName { get; }
        public int Rank { get; }
        public CraftItemKey? IngredientKey { get; }
        public long IngredientAmount { get; }
        public CraftItemKey? DismantleOutputKey { get; }
        public long DismantleOutputAmount { get; }
        public bool IsCraftable => IngredientKey.HasValue;
        public bool IsDismantlable => DismantleOutputKey.HasValue && DismantleOutputAmount > 0;
    }
}
