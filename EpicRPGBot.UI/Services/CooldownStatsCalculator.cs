using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class CooldownStatsCalculator
    {
        public static CooldownStatsSnapshot BuildSnapshot(
            CooldownDefinition[] definitions,
            Func<string, bool> isRunning)
        {
            var totalCount = 0;
            var totalRunning = 0;
            var rewardsCount = 0;
            var rewardsRunning = 0;
            var experienceCount = 0;
            var experienceRunning = 0;
            var progressCount = 0;
            var progressRunning = 0;

            foreach (var definition in definitions)
            {
                totalCount++;
                var running = isRunning(definition.CanonicalKey);
                if (running)
                {
                    totalRunning++;
                }

                switch (definition.Category)
                {
                    case CooldownCategory.Rewards:
                        rewardsCount++;
                        if (running) rewardsRunning++;
                        break;
                    case CooldownCategory.Experience:
                        experienceCount++;
                        if (running) experienceRunning++;
                        break;
                    case CooldownCategory.Progress:
                        progressCount++;
                        if (running) progressRunning++;
                        break;
                }
            }

            return new CooldownStatsSnapshot(
                totalRunning,
                totalCount,
                rewardsRunning,
                rewardsCount,
                experienceRunning,
                experienceCount,
                progressRunning,
                progressCount);
        }
    }
}
