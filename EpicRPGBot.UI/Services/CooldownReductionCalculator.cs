using System;

namespace EpicRPGBot.UI.Services
{
    internal static class CooldownReductionCalculator
    {
        public static TimeSpan? Reduce(TimeSpan? remaining, TimeSpan reduction)
        {
            if (!remaining.HasValue || reduction <= TimeSpan.Zero)
            {
                return remaining;
            }

            var next = remaining.Value - reduction;
            return next > TimeSpan.Zero ? next : (TimeSpan?)null;
        }
    }
}
