using System;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Services
{
    public static class TimeCookieMessageParser
    {
        private static readonly Regex AheadMinutesPattern = new Regex(
            @"(?<value>\d+)\s*(?:minute(?:\(s\))?s?|mins?)\s+ahead",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryParseReduction(string message, out TimeSpan reduction)
        {
            reduction = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(message) ||
                message.IndexOf("time cookie", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var match = AheadMinutesPattern.Match(message);
            if (!match.Success || !int.TryParse(match.Groups["value"].Value, out var minutes) || minutes <= 0)
            {
                return false;
            }

            reduction = TimeSpan.FromMinutes(minutes);
            return true;
        }
    }
}
