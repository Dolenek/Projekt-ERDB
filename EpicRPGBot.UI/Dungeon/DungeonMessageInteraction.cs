using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    internal static class DungeonMessageInteraction
    {
        public static DiscordMessageSnapshot FindMessageAfter(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string afterMessageId,
            Func<DiscordMessageSnapshot, bool> predicate)
        {
            if (snapshots == null || snapshots.Count == 0 || predicate == null)
            {
                return null;
            }

            var startIndex = 0;
            if (!string.IsNullOrWhiteSpace(afterMessageId))
            {
                for (var i = 0; i < snapshots.Count; i++)
                {
                    if (string.Equals(snapshots[i]?.Id, afterMessageId, StringComparison.Ordinal))
                    {
                        startIndex = i + 1;
                        break;
                    }
                }
            }

            for (var i = snapshots.Count - 1; i >= startIndex; i--)
            {
                if (predicate(snapshots[i]))
                {
                    return snapshots[i];
                }
            }

            return null;
        }

        public static bool HasButton(DiscordMessageSnapshot snapshot, string label)
        {
            return snapshot?.Buttons?.Any(button => LabelsMatch(button.Label, label)) == true;
        }

        public static bool LabelsMatch(string left, string right)
        {
            return string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLabel(string value)
        {
            return new string((value ?? string.Empty)
                .Trim()
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
    }
}
