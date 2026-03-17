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
            long ingredientAmount)
        {
            Key = key;
            DisplayName = displayName;
            CommandName = commandName;
            Rank = rank;
            IngredientKey = ingredientKey;
            IngredientAmount = ingredientAmount;
        }

        public CraftItemKey Key { get; }
        public string DisplayName { get; }
        public string CommandName { get; }
        public int Rank { get; }
        public CraftItemKey? IngredientKey { get; }
        public long IngredientAmount { get; }
        public bool IsCraftable => IngredientKey.HasValue;
    }
}
