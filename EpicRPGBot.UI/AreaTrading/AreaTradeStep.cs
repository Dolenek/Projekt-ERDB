using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI.AreaTrading
{
    public sealed class AreaTradeStep
    {
        private AreaTradeStep(AreaTradeStepKind kind, CraftItemKey? itemKey, string tradeLetter)
        {
            Kind = kind;
            ItemKey = itemKey;
            TradeLetter = tradeLetter ?? string.Empty;
        }

        public AreaTradeStepKind Kind { get; }
        public CraftItemKey? ItemKey { get; }
        public string TradeLetter { get; }

        public static AreaTradeStep DismantleAll(CraftItemKey itemKey)
        {
            return new AreaTradeStep(AreaTradeStepKind.DismantleAll, itemKey, string.Empty);
        }

        public static AreaTradeStep TradeAll(string tradeLetter)
        {
            return new AreaTradeStep(AreaTradeStepKind.TradeAll, null, tradeLetter);
        }
    }
}
