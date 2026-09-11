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
        private static readonly Regex LevelPattern = new Regex(
            @"\bLevel:\s*(?<value>[\d,]+)\b",
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

        public static bool TryParseLevel(string message, out int level)
        {
            level = 0;
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var match = LevelPattern.Match(message);
            var normalized = match.Success
                ? match.Groups["value"].Value.Replace(",", string.Empty)
                : string.Empty;
            return int.TryParse(normalized, out level) && level > 0;
        }
    }
}
