using System;
using System.Globalization;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordMessageChronology
    {
        public static bool IsAfter(string replyId, string outgoingId)
        {
            if (!TryReadId(replyId, out var replyPrefix, out var replySequence) ||
                !TryReadId(outgoingId, out var outgoingPrefix, out var outgoingSequence)) return false;
            return string.Equals(replyPrefix, outgoingPrefix, StringComparison.Ordinal) && replySequence > outgoingSequence;
        }

        public static ulong GetSequence(string messageId)
        {
            TryReadId(messageId, out _, out var sequence);
            return sequence;
        }

        private static bool TryReadId(string messageId, out string prefix, out ulong sequence)
        {
            prefix = string.Empty;
            sequence = 0;
            if (string.IsNullOrWhiteSpace(messageId)) return false;
            var separator = messageId.LastIndexOf('-');
            prefix = messageId.Substring(0, separator + 1);
            return ulong.TryParse(messageId.Substring(separator + 1), NumberStyles.None,
                CultureInfo.InvariantCulture, out sequence);
        }
    }
}
