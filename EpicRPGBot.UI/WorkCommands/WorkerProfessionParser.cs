using System.Globalization;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.WorkCommands
{
    public static class WorkerProfessionParser
    {
        private static readonly Regex WorkerLevelPattern = new Regex(
            @"\bWorker\s+Lv\s*(?<value>[\d,]+)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool TryParseLevel(string message, out int workerLevel)
        {
            workerLevel = 0;
            var match = WorkerLevelPattern.Match(message ?? string.Empty);
            if (!match.Success)
            {
                return false;
            }

            var normalized = match.Groups["value"].Value.Replace(",", string.Empty);
            return int.TryParse(
                normalized,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out workerLevel) && workerLevel > 0;
        }
    }
}
