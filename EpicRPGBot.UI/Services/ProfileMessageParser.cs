using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Services
{
    public static class ProfileMessageParser
    {
        private static readonly Regex MaxAreaPattern = new Regex(
            @"Area:\s*\d+\s*\(Max:\s*(?<value>\d+)\)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
