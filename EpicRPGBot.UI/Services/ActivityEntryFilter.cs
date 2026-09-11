using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public static class ActivityEntryFilter
    {
        public static bool MatchesMessage(MessageItem message, string query)
        {
            if (message == null)
            {
                return false;
            }

            return Contains(message.ToString(), query);
        }

        public static bool MatchesLog(LogEntry entry, string query, LogKind? kind)
        {
            if (entry == null || (kind.HasValue && entry.Kind != kind.Value))
            {
                return false;
            }

            return Contains(entry.ToString(), query);
        }

        private static bool Contains(string value, string query)
        {
            return string.IsNullOrWhiteSpace(query) ||
                   value.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
