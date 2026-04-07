using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonBattleStateParser
    {
        public DungeonBattleState Parse(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string playerName)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return new DungeonBattleState(false, false, false, false, string.Empty, string.Empty, null);
            }

            var deletePrompt = FindDeletePrompt(snapshots);
            var victory = snapshots.Any(snapshot => IsVictory(snapshot?.RenderedText) || IsVictory(snapshot?.Text));
            var failure = snapshots.Any(snapshot => IsFailure(snapshot?.RenderedText) || IsFailure(snapshot?.Text));
            var battleMessage = FindLatestBattleMessage(snapshots);
            var shouldBite = battleMessage != null && IsPlayerTurn(battleMessage, playerName);

            return new DungeonBattleState(
                battleMessage != null || victory || failure,
                shouldBite,
                victory,
                failure,
                BuildActivitySignature(snapshots),
                BuildActionSignature(battleMessage),
                deletePrompt);
        }

        private static DiscordMessageSnapshot FindDeletePrompt(IReadOnlyList<DiscordMessageSnapshot> snapshots)
        {
            for (var i = snapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = snapshots[i];
                if (snapshot?.Buttons?.Any(button => LabelsMatch(button.Label, "Delete dungeon channel")) == true)
                {
                    return snapshot;
                }
            }

            return null;
        }

        private static DiscordMessageSnapshot FindLatestBattleMessage(IReadOnlyList<DiscordMessageSnapshot> snapshots)
        {
            for (var i = snapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = snapshots[i];
                if (LooksLikeBattleMessage(snapshot))
                {
                    return snapshot;
                }
            }

            return null;
        }

        private static bool LooksLikeBattleMessage(DiscordMessageSnapshot snapshot)
        {
            var text = snapshot?.RenderedText ?? string.Empty;
            return text.IndexOf("YOU HAVE ENCOUNTERED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("What will you do", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("turn!", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   snapshot?.Buttons?.Any(button => IsBattleButton(button.Label)) == true;
        }

        private static bool IsBattleButton(string label)
        {
            return LabelsMatch(label, "BITE") ||
                   LabelsMatch(label, "STAB") ||
                   LabelsMatch(label, "POWER") ||
                   LabelsMatch(label, "HEALING SPELL");
        }

        private static bool IsPlayerTurn(DiscordMessageSnapshot snapshot, string playerName)
        {
            var normalizedPlayer = NormalizeName(playerName);
            if (string.IsNullOrWhiteSpace(normalizedPlayer))
            {
                return false;
            }

            var normalizedText = NormalizeText(snapshot?.RenderedText ?? snapshot?.Text);
            return normalizedText.Contains("its" + normalizedPlayer + "sturn") ||
                   normalizedText.Contains("whatwillyoudo" + normalizedPlayer);
        }

        private static bool IsVictory(string text)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   (text.IndexOf("ALL PLAYERS WON", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    text.IndexOf("Thanks for using our dungeon system", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsFailure(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            return text.IndexOf("ALL PLAYERS LOST", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("GAME OVER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("dungeon failed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string BuildActivitySignature(IReadOnlyList<DiscordMessageSnapshot> snapshots)
        {
            return string.Join(
                "||",
                snapshots
                    .Skip(Math.Max(0, snapshots.Count - 3))
                    .Select(snapshot => (snapshot?.Id ?? string.Empty) + "|" + (snapshot?.RenderedText ?? snapshot?.Text ?? string.Empty)));
        }

        private static string BuildActionSignature(DiscordMessageSnapshot snapshot)
        {
            return snapshot == null
                ? string.Empty
                : (snapshot.Id ?? string.Empty) + "|" + (snapshot.RenderedText ?? snapshot.Text ?? string.Empty);
        }

        private static bool LabelsMatch(string left, string right)
        {
            return string.Equals(NormalizeText(left), NormalizeText(right), StringComparison.Ordinal);
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

        private static string NormalizeText(string value)
        {
            return new string((value ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
    }
}
