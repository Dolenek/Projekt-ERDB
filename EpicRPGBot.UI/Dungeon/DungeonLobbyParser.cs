using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonLobbyParser
    {
        public DiscordMessageMention FindPartnerMention(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string playerName)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return null;
            }

            var selfKey = NormalizeName(playerName);
            for (var i = snapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = snapshots[i];
                if (!LooksLikeLobbyMessage(snapshot))
                {
                    continue;
                }

                var listedPlayers = ParseListedPlayers(snapshot.RenderedText);
                var partner = ResolveMentionBackedPartner(snapshot.Mentions, listedPlayers, selfKey) ??
                              ResolveTextBackedPartner(listedPlayers, selfKey);
                if (partner != null)
                {
                    return partner;
                }
            }

            return null;
        }

        private static bool LooksLikeLobbyMessage(DiscordMessageSnapshot snapshot)
        {
            var text = snapshot?.RenderedText ?? string.Empty;
            return text.IndexOf("Players listed", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   text.IndexOf("Dungeon", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static DiscordMessageMention ResolveMentionBackedPartner(
            IReadOnlyList<DiscordMessageMention> mentions,
            IReadOnlyList<ListedPlayerEntry> listedPlayers,
            string selfKey)
        {
            if (mentions == null || mentions.Count == 0)
            {
                return null;
            }

            var orderedMentions = mentions
                .Where(mention => mention != null && !string.IsNullOrWhiteSpace(mention.Label))
                .OrderByDescending(mention => !string.IsNullOrWhiteSpace(mention.UserId))
                .ToList();
            foreach (var mention in orderedMentions)
            {
                var listedPlayer = listedPlayers.FirstOrDefault(candidate =>
                    string.Equals(NormalizeName(candidate.MentionLabel), NormalizeName(mention.Label), StringComparison.Ordinal));
                if (IsSelfMention(mention.Label, listedPlayer?.PlayerName, selfKey))
                {
                    continue;
                }

                return mention;
            }

            return null;
        }

        private static DiscordMessageMention ResolveTextBackedPartner(
            IReadOnlyList<ListedPlayerEntry> listedPlayers,
            string selfKey)
        {
            var partner = listedPlayers.FirstOrDefault(candidate => !IsSelfMention(candidate.MentionLabel, candidate.PlayerName, selfKey));
            if (partner == null)
            {
                return null;
            }

            var mentionHandle = ExtractMentionHandle(partner.MentionLabel);
            var playerTag = partner.PlayerName;
            var commandToken = SelectPreferredToken(mentionHandle, playerTag);
            var alternateToken = SelectAlternateToken(commandToken, mentionHandle, playerTag);
            return new DiscordMessageMention(
                partner.MentionLabel,
                string.Empty,
                commandToken,
                alternateToken);
        }

        private static bool IsSelfMention(string mentionLabel, string playerName, string selfKey)
        {
            return string.Equals(NormalizeName(mentionLabel), selfKey, StringComparison.Ordinal) ||
                   string.Equals(NormalizeName(playerName), selfKey, StringComparison.Ordinal);
        }

        private static IReadOnlyList<ListedPlayerEntry> ParseListedPlayers(string renderedText)
        {
            var entries = new List<ListedPlayerEntry>();
            var lines = (renderedText ?? string.Empty)
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToArray();
            var playersIndex = Array.FindIndex(lines, line => line.IndexOf("Players listed", StringComparison.OrdinalIgnoreCase) >= 0);
            if (playersIndex < 0)
            {
                return entries;
            }

            for (var i = playersIndex + 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.IndexOf("Recommended trades", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf("This channel will be deleted", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    break;
                }

                if (!line.StartsWith("@", StringComparison.Ordinal))
                {
                    continue;
                }

                var separatorIndex = FindPlayerSeparatorIndex(line);
                var mentionLabel = separatorIndex > 0 ? line.Substring(0, separatorIndex).Trim() : line.Trim();
                var playerName = separatorIndex > 0 && separatorIndex + 3 <= line.Length
                    ? line.Substring(separatorIndex + 3).Trim()
                    : string.Empty;
                entries.Add(new ListedPlayerEntry(mentionLabel, playerName));
            }

            return entries;
        }

        private static int FindPlayerSeparatorIndex(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return -1;
            }

            var spacedSeparatorIndex = line.LastIndexOf(" - ", StringComparison.Ordinal);
            if (spacedSeparatorIndex >= 0)
            {
                return spacedSeparatorIndex;
            }

            return line.LastIndexOf('-');
        }

        private static string ExtractMentionHandle(string mentionLabel)
        {
            return (mentionLabel ?? string.Empty).Trim().TrimStart('@');
        }

        private static string SelectPreferredToken(string mentionHandle, string playerTag)
        {
            if (IsPlainCommandToken(mentionHandle))
            {
                return mentionHandle;
            }

            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                return playerTag;
            }

            return mentionHandle;
        }

        private static string SelectAlternateToken(string selectedToken, string mentionHandle, string playerTag)
        {
            if (IsDistinctToken(selectedToken, mentionHandle) && IsPlainCommandToken(mentionHandle))
            {
                return mentionHandle;
            }

            if (IsDistinctToken(selectedToken, playerTag))
            {
                return playerTag;
            }

            return string.Empty;
        }

        private static bool IsDistinctToken(string selectedToken, string candidate)
        {
            return !string.IsNullOrWhiteSpace(candidate) &&
                   !string.Equals(selectedToken ?? string.Empty, candidate, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPlainCommandToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var character in value)
            {
                var isAsciiLetter = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z';
                var isDigit = character >= '0' && character <= '9';
                if (!isAsciiLetter && !isDigit && character != '_' && character != '.')
                {
                    return false;
                }
            }

            return true;
        }

        private static string NormalizeName(string value)
        {
            return new string((value ?? string.Empty)
                .Trim()
                .TrimStart('@')
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private sealed class ListedPlayerEntry
        {
            public ListedPlayerEntry(string mentionLabel, string playerName)
            {
                MentionLabel = mentionLabel ?? string.Empty;
                PlayerName = playerName ?? string.Empty;
            }

            public string MentionLabel { get; }
            public string PlayerName { get; }
        }
    }
}
