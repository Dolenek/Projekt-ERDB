#nullable enable
using System;
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
            if (!GuardIncidentTracker.ContainsGuardClear(message))
            {
                return false;
            }

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
