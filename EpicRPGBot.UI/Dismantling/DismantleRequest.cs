namespace EpicRPGBot.UI.Dismantling
{
    public sealed class DismantleRequest
    {
        public DismantleRequest(Crafting.CraftItemKey itemKey, bool useAll, long amount)
        {
            ItemKey = itemKey;
            UseAll = useAll;
            Amount = amount;
        }

        public Crafting.CraftItemKey ItemKey { get; }
        public bool UseAll { get; }
        public long Amount { get; }
        public string CommandAmount => UseAll ? "all" : Amount.ToString();
    }
}
