using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.WorkCommands
{
    public static class ActiveWorkPotionParser
    {
        private static readonly Regex BoostReplyPattern = new Regex(
            @"(?:[—-]\s*boosts\b|your\s+active\s+boosts)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex FishPotionPattern = new Regex(
            @"\bFish\s+potion\s*:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex WoodPotionPattern = new Regex(
            @"\bWood\s+potion\s*:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryParse(
            string message,
            out bool fishPotionActive,
            out bool woodPotionActive)
        {
            fishPotionActive = false;
            woodPotionActive = false;
            if (!BoostReplyPattern.IsMatch(message ?? string.Empty))
            {
                return false;
            }

            fishPotionActive = FishPotionPattern.IsMatch(message);
            woodPotionActive = WoodPotionPattern.IsMatch(message);
            return true;
        }
    }
}
