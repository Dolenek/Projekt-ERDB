using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.WorkCommands
{
    public static class AutoBestWorkCommandPlanner
    {
        private const decimal TwentyMillionCoins = 20000000m;
        private const decimal ThirtyMillionCoins = 30000000m;

        private static readonly AscendedBand[] WithoutBothPotions =
        {
            new AscendedBand(101, "dynamite", "dynamite", "dynamite", "dynamite", "dynamite", "chainsaw"),
            new AscendedBand(106, "dynamite", "dynamite", "greenhouse", "dynamite", "dynamite", "chainsaw"),
            new AscendedBand(108, "dynamite", "dynamite", "greenhouse", "dynamite", "greenhouse", "chainsaw"),
            new AscendedBand(114, "dynamite", "dynamite", "greenhouse", "chainsaw", "greenhouse", "chainsaw"),
            new AscendedBand(int.MaxValue, "dynamite", "chainsaw", "greenhouse", "chainsaw", "greenhouse", "chainsaw")
        };

        private static readonly AscendedBand[] WithBothPotions =
        {
            new AscendedBand(108, "dynamite", "dynamite", "dynamite", "dynamite", "dynamite", "chainsaw"),
            new AscendedBand(113, "dynamite", "dynamite", "greenhouse", "dynamite", "dynamite", "chainsaw"),
            new AscendedBand(117, "dynamite", "dynamite", "greenhouse", "dynamite", "greenhouse", "chainsaw"),
            new AscendedBand(122, "dynamite", "dynamite", "greenhouse", "chainsaw", "greenhouse", "chainsaw"),
            new AscendedBand(int.MaxValue, "dynamite", "chainsaw", "greenhouse", "chainsaw", "greenhouse", "chainsaw")
        };

        public static AutoBestWorkCommandPlan Build(
            decimal totalCoins,
            int timeTravels,
            bool ascended,
            int? workerLevel,
            bool fishPotionActive,
            bool woodPotionActive)
        {
            if (!ascended || !workerLevel.HasValue || workerLevel.Value < 100)
            {
                return new AutoBestWorkCommandPlan(
                    BuildRegular(totalCoins, timeTravels),
                    AutoBestWorkCommandMode.Regular);
            }

            var hasBothPotions = fishPotionActive && woodPotionActive;
            return new AutoBestWorkCommandPlan(
                BuildAscended(workerLevel.Value, hasBothPotions),
                hasBothPotions
                    ? AutoBestWorkCommandMode.AscendedWithPotions
                    : AutoBestWorkCommandMode.AscendedWithoutPotions);
        }

        private static IReadOnlyDictionary<int, string> BuildRegular(
            decimal totalCoins,
            int timeTravels)
        {
            var selections = new Dictionary<int, string>();
            for (var area = AreaWorkCommandSettings.MinimumArea;
                 area <= AreaWorkCommandSettings.MaximumArea;
                 area++)
            {
                selections[area] = "rpg " + ResolveRegularCommand(area, totalCoins, timeTravels);
            }

            return selections;
        }

        private static string ResolveRegularCommand(int area, decimal totalCoins, int timeTravels)
        {
            if (area <= 2) return "chop";
            if (area <= 5) return "axe";

            var needsEarlyCoins = totalCoins < TwentyMillionCoins || timeTravels <= 2;
            if (area <= 7) return needsEarlyCoins ? "pickaxe" : "ladder";
            if (area == 8) return needsEarlyCoins ? "pickaxe" : "bowsaw";
            if (area == 9) return totalCoins < TwentyMillionCoins ? "pickaxe" : "chainsaw";
            if (area <= 11) return totalCoins < ThirtyMillionCoins ? "drill" : "chainsaw";
            return totalCoins < ThirtyMillionCoins ? "dynamite" : "chainsaw";
        }

        private static IReadOnlyDictionary<int, string> BuildAscended(
            int workerLevel,
            bool hasBothPotions)
        {
            var bands = hasBothPotions ? WithBothPotions : WithoutBothPotions;
            var band = bands.First(candidate => workerLevel <= candidate.MaximumWorkerLevel);
            var selections = new Dictionary<int, string>();
            for (var area = AreaWorkCommandSettings.MinimumArea;
                 area <= AreaWorkCommandSettings.MaximumArea;
                 area++)
            {
                selections[area] = "rpg " + band.Commands[ResolveAreaGroup(area)];
            }

            return selections;
        }

        private static int ResolveAreaGroup(int area)
        {
            if (area <= 3) return 0;
            if (area <= 5) return 1;
            if (area <= 7) return 2;
            if (area == 8) return 3;
            return area == 9 ? 4 : 5;
        }

        private sealed class AscendedBand
        {
            public AscendedBand(int maximumWorkerLevel, params string[] commands)
            {
                if (commands == null || commands.Length != 6)
                    throw new ArgumentException("Six area-group commands are required.", nameof(commands));
                MaximumWorkerLevel = maximumWorkerLevel;
                Commands = commands;
            }

            public int MaximumWorkerLevel { get; }

            public string[] Commands { get; }
        }
    }
}
