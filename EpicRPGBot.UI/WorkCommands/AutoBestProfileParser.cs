using System.Globalization;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.WorkCommands
{
    public static class AutoBestProfileParser
    {
        private static readonly Regex CoinsPattern = CreateMoneyPattern("Coins");
        private static readonly Regex BankPattern = CreateMoneyPattern("Bank");
        private static readonly Regex TimeTravelsPattern = new Regex(
            @"\bTime\s+travels\s*:\s*(?<value>[\d,]+)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryParse(string message, out AutoBestWorkProfile profile)
        {
            profile = null;
            if (!TryParseMoney(CoinsPattern, message, out var coins) ||
                !TryParseMoney(BankPattern, message, out var bank) ||
                !TryParseTimeTravels(message, out var timeTravels))
            {
                return false;
            }

            profile = new AutoBestWorkProfile(coins, bank, timeTravels);
            return true;
        }

        private static Regex CreateMoneyPattern(string label)
        {
            return new Regex(
                @"(?:^|\r?\n)[^\p{L}\d\r\n]*" + label +
                @"\s*:\s*(?<value>\d(?:[\d,\u00A0\u202F ]*\d)?)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        }

        private static bool TryParseMoney(Regex pattern, string message, out decimal value)
        {
            value = 0;
            var match = pattern.Match(message ?? string.Empty);
            return match.Success && TryParseDigits(match.Groups["value"].Value, out value);
        }

        private static bool TryParseTimeTravels(string message, out int timeTravels)
        {
            timeTravels = 0;
            var match = TimeTravelsPattern.Match(message ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var digits = Regex.Replace(match.Groups["value"].Value, @"\D", string.Empty);
            return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out timeTravels);
        }

        private static bool TryParseDigits(string value, out decimal result)
        {
            var digits = Regex.Replace(value ?? string.Empty, @"\D", string.Empty);
            return decimal.TryParse(
                digits,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out result);
        }
    }
}
