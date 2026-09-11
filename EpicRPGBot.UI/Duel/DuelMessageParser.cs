using System;
using System.Linq;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelMessageParser
    {
        private static readonly Regex HeaderPattern = new Regex(
            @"^\s*(?<name>.+?)\s+[—-]\s+duel\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        private static readonly Regex TargetPattern = new Regex(
            @"Will\s+you\s+accept,\s*(?<name>.+?)\s*\?",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public DuelMessageClassification Parse(DiscordMessageSnapshot message)
        {
            var text = GetText(message);
            if (string.IsNullOrWhiteSpace(text))
            {
                return Unknown(message);
            }

            var initiator = MatchName(HeaderPattern, text);
            var target = MatchName(TargetPattern, text);
            if (Contains(text, "currently busy") || Contains(text, "user is busy") ||
                Contains(text, "middle of a command") || Contains(text, "already in a duel"))
            {
                return Create(DuelMessageKind.Busy, message, initiator, target);
            }

            if (Contains(text, "duel cancelled") || Contains(text, "duel canceled"))
            {
                return Create(DuelMessageKind.Cancelled, message, initiator, target);
            }

            if (IsResult(text))
            {
                return Create(DuelMessageKind.Result, message, initiator, target);
            }

            if (Contains(text, "choose the weapon"))
            {
                return Create(DuelMessageKind.WeaponChoice, message, initiator, target);
            }

            if (Contains(text, "sent a duel request to") && HasButton(message, "yes"))
            {
                return Create(DuelMessageKind.IncomingRequest, message, initiator, target);
            }

            var hasInitiatorConfirmation = Contains(text, "are you sure") || Contains(text, "confirm") ||
                                           (Contains(text, "duel") && string.IsNullOrWhiteSpace(target));
            if (hasInitiatorConfirmation && HasButton(message, "yes"))
            {
                return Create(DuelMessageKind.InitiatorConfirmation, message, initiator, target);
            }

            return Unknown(message);
        }

        public bool BelongsToPlayer(DuelMessageClassification classification, string playerName)
        {
            if (classification == null || string.IsNullOrWhiteSpace(playerName))
            {
                return false;
            }

            var text = GetText(classification.Message);
            return text.IndexOf(playerName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsResult(string text)
        {
            var terminal = Contains(text, "won!") || Contains(text, "it's a tie") ||
                           Contains(text, "it is a tie") || Contains(text, "draw!");
            return terminal && (Contains(text, "duel") || Contains(text, "reward:"));
        }

        private static bool HasButton(DiscordMessageSnapshot message, string label)
        {
            return message?.Buttons.Any(button => Normalize(button.Label) == Normalize(label)) == true;
        }

        private static string GetText(DiscordMessageSnapshot message)
        {
            return (message?.RenderedText ?? string.Empty) + "\n" + (message?.Text ?? string.Empty);
        }

        private static string MatchName(Regex pattern, string text)
        {
            var match = pattern.Match(text);
            return match.Success ? match.Groups["name"].Value.Trim() : string.Empty;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Normalize(string value)
        {
            return new string((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static DuelMessageClassification Create(
            DuelMessageKind kind,
            DiscordMessageSnapshot message,
            string initiator,
            string target)
        {
            return new DuelMessageClassification(kind, message, initiator, target);
        }

        private static DuelMessageClassification Unknown(DiscordMessageSnapshot message)
        {
            return new DuelMessageClassification(DuelMessageKind.Unknown, message);
        }
    }
}
