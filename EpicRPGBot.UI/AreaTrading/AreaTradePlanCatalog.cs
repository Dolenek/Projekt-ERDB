using System.Collections.Generic;
using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI.AreaTrading
{
    public sealed class AreaTradePlanCatalog
    {
        private readonly IReadOnlyDictionary<int, IReadOnlyList<AreaTradeStep>> _plans;

        public AreaTradePlanCatalog(IReadOnlyDictionary<int, IReadOnlyList<AreaTradeStep>> plans)
        {
            _plans = plans ?? new Dictionary<int, IReadOnlyList<AreaTradeStep>>();
        }

        public static AreaTradePlanCatalog Default { get; } = new AreaTradePlanCatalog(
            new Dictionary<int, IReadOnlyList<AreaTradeStep>>
            {
                [3] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.Banana),
                    AreaTradeStep.DismantleAll(CraftItemKey.UltraLog),
                    AreaTradeStep.TradeAll("C"),
                    AreaTradeStep.TradeAll("B")
                },
                [5] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.UltraLog),
                    AreaTradeStep.DismantleAll(CraftItemKey.EpicFish),
                    AreaTradeStep.TradeAll("E"),
                    AreaTradeStep.TradeAll("A"),
                    AreaTradeStep.TradeAll("D")
                },
                [7] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.Banana),
                    AreaTradeStep.TradeAll("C")
                },
                [8] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.UltraLog),
                    AreaTradeStep.DismantleAll(CraftItemKey.EpicFish),
                    AreaTradeStep.TradeAll("E"),
                    AreaTradeStep.TradeAll("A"),
                    AreaTradeStep.TradeAll("D")
                },
                [9] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.MegaLog),
                    AreaTradeStep.DismantleAll(CraftItemKey.Banana),
                    AreaTradeStep.TradeAll("E"),
                    AreaTradeStep.TradeAll("C"),
                    AreaTradeStep.TradeAll("B")
                },
                [10] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.Banana),
                    AreaTradeStep.TradeAll("C")
                },
                [11] = new[]
                {
                    AreaTradeStep.TradeAll("E")
                },
                [15] = new[]
                {
                    AreaTradeStep.DismantleAll(CraftItemKey.EpicFish),
                    AreaTradeStep.DismantleAll(CraftItemKey.Banana),
                    AreaTradeStep.TradeAll("E"),
                    AreaTradeStep.TradeAll("A"),
                    AreaTradeStep.TradeAll("C")
                }
            });

        public bool TryGet(int area, out IReadOnlyList<AreaTradeStep> steps)
        {
            return _plans.TryGetValue(area, out steps);
        }
    }
}
