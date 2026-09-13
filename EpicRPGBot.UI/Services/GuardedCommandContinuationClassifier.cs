#nullable enable
using System;
using System.Collections.Generic;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class GuardedCommandContinuationClassifier
    {
        public static bool IsInlineContinuation(
            string interruptedCommand,
            DiscordMessageSnapshot clearReply)
        {
            if (string.IsNullOrWhiteSpace(interruptedCommand) || clearReply == null)
            {
                return false;
            }

            var message = CombineMessageText(clearReply);
            return GuardIncidentTracker.ContainsGuardClear(message) &&
                IsCommandContinuation(interruptedCommand, clearReply);
        }

        public static bool IsCommandContinuation(
            string interruptedCommand,
            DiscordMessageSnapshot reply)
        {
            if (string.IsNullOrWhiteSpace(interruptedCommand) || reply == null)
            {
                return false;
            }

            var message = CombineMessageText(reply);
            if (TrackedCommandResponseClassifier.TryParseWaitAtLeast(message, out _))
            {
                return true;
            }

            var command = interruptedCommand.Trim();
            if (StartsWithCommand(command, "rpg use time cookie"))
            {
                return TimeCookieMessageParser.TryParseReduction(message, out _);
            }

            if (StartsWithCommand(command, "rpg cd"))
            {
                return message.IndexOf("cooldowns", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return MatchesTrackedCommandReply(command, message);
        }

        public static DiscordMessageSnapshot? FindContinuation(
            string interruptedCommand,
            string clearMessageId,
            IReadOnlyList<DiscordMessageSnapshot> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return null;
            }

            var clearIndex = FindMessageIndex(snapshots, clearMessageId);
            if (clearIndex < 0)
            {
                return null;
            }

            var editedClear = snapshots[clearIndex];
            if (IsInlineContinuation(interruptedCommand, editedClear))
            {
                return editedClear;
            }

            var epicAuthorId = editedClear.AuthorId;
            var previousMessageWasEpic = true;
            for (var index = clearIndex + 1; index < snapshots.Count; index++)
            {
                var candidate = snapshots[index];
                previousMessageWasEpic = IsFromEpicMessageGroup(
                    candidate,
                    epicAuthorId,
                    previousMessageWasEpic);
                if (previousMessageWasEpic &&
                    IsCommandContinuation(interruptedCommand, candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static bool MatchesTrackedCommandReply(string command, string message)
        {
            if (!TrackedCommandResponseClassifier.TryInferKind(message, out var responseKind))
            {
                return false;
            }

            return !TryGetExpectedKind(command, out var expectedKind) ||
                responseKind == expectedKind;
        }

        private static bool TryGetExpectedKind(string command, out TrackedCommandKind kind)
        {
            if (ConfiguredWorkCommandCatalog.IsWorkCommand(command, string.Empty))
            {
                kind = TrackedCommandKind.Work;
                return true;
            }

            var mappings = new[]
            {
                ("rpg daily", TrackedCommandKind.Daily),
                ("rpg weekly", TrackedCommandKind.Weekly),
                ("rpg card hand", TrackedCommandKind.CardHand),
                ("rpg hunt", TrackedCommandKind.Hunt),
                ("rpg adv", TrackedCommandKind.Adventure),
                ("rpg tr", TrackedCommandKind.Training),
                ("rpg farm", TrackedCommandKind.Farm),
                ("rpg buy ed lb", TrackedCommandKind.Lootbox)
            };

            foreach (var mapping in mappings)
            {
                if (StartsWithCommand(command, mapping.Item1))
                {
                    kind = mapping.Item2;
                    return true;
                }
            }

            kind = TrackedCommandKind.Hunt;
            return false;
        }

        private static bool StartsWithCommand(string command, string expectedCommand)
        {
            return command.Equals(expectedCommand, StringComparison.OrdinalIgnoreCase) ||
                command.StartsWith(expectedCommand + " ", StringComparison.OrdinalIgnoreCase);
        }

        private static int FindMessageIndex(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string messageId)
        {
            for (var index = 0; index < snapshots.Count; index++)
            {
                if (string.Equals(snapshots[index]?.Id, messageId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        private static bool IsFromEpicMessageGroup(
            DiscordMessageSnapshot snapshot,
            string epicAuthorId,
            bool previousMessageWasEpic)
        {
            if (snapshot == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.Author))
            {
                return snapshot.Author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    AuthorIdsMatch(snapshot.AuthorId, epicAuthorId);
            }

            if (string.IsNullOrWhiteSpace(snapshot.AuthorId) ||
                string.IsNullOrWhiteSpace(epicAuthorId))
            {
                return previousMessageWasEpic;
            }

            return AuthorIdsMatch(snapshot.AuthorId, epicAuthorId);
        }

        private static bool AuthorIdsMatch(string candidateAuthorId, string epicAuthorId)
        {
            return !string.IsNullOrWhiteSpace(candidateAuthorId) &&
                !string.IsNullOrWhiteSpace(epicAuthorId) &&
                string.Equals(candidateAuthorId, epicAuthorId, StringComparison.Ordinal);
        }

        private static string CombineMessageText(DiscordMessageSnapshot snapshot)
        {
            var text = snapshot.Text ?? string.Empty;
            var renderedText = snapshot.RenderedText ?? string.Empty;
            return text.Equals(renderedText, StringComparison.Ordinal)
                ? text
                : text + Environment.NewLine + renderedText;
        }
    }
}
