using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.CardHand
{
    public static class CardHandRewardCatalog
    {
        private static readonly IReadOnlyDictionary<CardHandKind, IReadOnlyDictionary<CardRewardKind, int>> Rewards = Build();

        public static IReadOnlyDictionary<CardRewardKind, int> Get(CardHandKind kind)
        {
            return Rewards[kind];
        }

        private static IReadOnlyDictionary<CardHandKind, IReadOnlyDictionary<CardRewardKind, int>> Build()
        {
            return new Dictionary<CardHandKind, IReadOnlyDictionary<CardRewardKind, int>>
            {
                [CardHandKind.AceExtravaganza] = Bundle((CardRewardKind.TimeCapsule, 10), (CardRewardKind.TimeCookie, 5000), (CardRewardKind.EternalLootbox, 1), (CardRewardKind.RoundCard, 42)),
                [CardHandKind.RoyalHeartedFlush] = Bundle((CardRewardKind.TimeCapsule, 3), (CardRewardKind.TimeCookie, 1200), (CardRewardKind.GodlyLootbox, 4), (CardRewardKind.OmegaLootbox, 30), (CardRewardKind.RoundCard, 15)),
                [CardHandKind.RoyalFlush] = Bundle((CardRewardKind.TimeCapsule, 1), (CardRewardKind.TimeCookie, 800), (CardRewardKind.GodlyLootbox, 2), (CardRewardKind.OmegaLootbox, 20), (CardRewardKind.RoundCard, 10)),
                [CardHandKind.AceGala] = Bundle((CardRewardKind.TimeCapsule, 1), (CardRewardKind.TimeCookie, 600), (CardRewardKind.GodlyLootbox, 1), (CardRewardKind.OmegaLootbox, 15), (CardRewardKind.RoundCard, 5)),
                [CardHandKind.StraightFlush] = Bundle((CardRewardKind.TimeCookie, 450), (CardRewardKind.GodlyLootbox, 1), (CardRewardKind.OmegaLootbox, 8), (CardRewardKind.RoundCard, 5)),
                [CardHandKind.FourOfAKind] = Bundle((CardRewardKind.TimeCookie, 250), (CardRewardKind.OmegaLootbox, 4), (CardRewardKind.Flask, 14), (CardRewardKind.RoundCard, 3)),
                [CardHandKind.FullHouse] = Bundle((CardRewardKind.TimeCookie, 160), (CardRewardKind.OmegaLootbox, 3), (CardRewardKind.Flask, 12), (CardRewardKind.RoundCard, 3)),
                [CardHandKind.GameOfKings] = Bundle((CardRewardKind.TimeCookie, 140), (CardRewardKind.OmegaLootbox, 3), (CardRewardKind.Flask, 10), (CardRewardKind.RoundCard, 2)),
                [CardHandKind.Flush] = Bundle((CardRewardKind.TimeCookie, 120), (CardRewardKind.OmegaLootbox, 2), (CardRewardKind.Flask, 8), (CardRewardKind.RoundCard, 2)),
                [CardHandKind.UnbreakableFortress] = Bundle((CardRewardKind.TimeCookie, 90), (CardRewardKind.GuildRing, 210), (CardRewardKind.Flask, 6), (CardRewardKind.RoundCard, 1)),
                [CardHandKind.Straight] = Bundle((CardRewardKind.TimeCookie, 70), (CardRewardKind.GuildRing, 180), (CardRewardKind.Flask, 4), (CardRewardKind.RoundCard, 1)),
                [CardHandKind.ThreeOfAKind] = Bundle((CardRewardKind.TimeCookie, 35), (CardRewardKind.GuildRing, 120), (CardRewardKind.Flask, 2), (CardRewardKind.ArenaCookie, 160)),
                [CardHandKind.TwoPairs] = Bundle((CardRewardKind.TimeCookie, 25), (CardRewardKind.GuildRing, 80), (CardRewardKind.ArenaCookie, 100)),
                [CardHandKind.Pair] = Bundle((CardRewardKind.TimeCookie, 10), (CardRewardKind.GuildRing, 25), (CardRewardKind.ArenaCookie, 30)),
                [CardHandKind.RandomCards] = Bundle((CardRewardKind.ArenaCookie, 10))
            };
        }

        private static IReadOnlyDictionary<CardRewardKind, int> Bundle(params (CardRewardKind Kind, int Amount)[] rewards)
        {
            var result = new Dictionary<CardRewardKind, int>();
            foreach (var reward in rewards) result[reward.Kind] = reward.Amount;
            return result;
        }
    }
}
