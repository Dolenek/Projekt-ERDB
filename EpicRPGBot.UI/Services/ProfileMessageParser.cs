using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Services
{
    public static class ProfileMessageParser
    {
        private static readonly Regex PlayerNamePattern = new Regex(
            @"^\s*(?<value>.+?)\s+[—-]\s+profile\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        private static readonly Regex MaxAreaPattern = new Regex(
            @"Area:\s*\d+\s*\(Max:\s*(?<value>\d+)\)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryParsePlayerName(string message, out string playerName)
        {
            playerName = string.Empty;
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var match = PlayerNamePattern.Match(message);
            if (!match.Success)
            {
                return false;
            }

            playerName = match.Groups["value"].Value.Trim();
            return !string.IsNullOrWhiteSpace(playerName);
        }

        public static bool TryParseMaxArea(string message, out int maxArea)
        {
            maxArea = 0;
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var match = MaxAreaPattern.Match(message);
            if (!match.Success)
            {
                return false;
            }

            return int.TryParse(match.Groups["value"].Value, out maxArea) && maxArea > 0;
        }
    }
}
