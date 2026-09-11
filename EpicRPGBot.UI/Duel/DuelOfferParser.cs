using System;
using System.Linq;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelOfferParser
    {
        private static readonly Regex OfferLinePattern = new Regex(
            @"^\s*(?<level>[\d,]+)\s+(?<tokens>[^\r\n]+)\s*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public bool TryParseEligibleCf(
            DiscordMessageSnapshot message,
            int playerLevel,
            DateTimeOffset cutoffUtc,
            string selfAuthorId,
            string selfAuthorName,
            out int opponentLevel)
        {
            return TryParseEligibleCf(
                message,
                playerLevel,
                cutoffUtc,
                selfAuthorId,
                selfAuthorName,
                out opponentLevel,
                out _);
        }

        public bool TryParseEligibleCf(
            DiscordMessageSnapshot message,
            int playerLevel,
            DateTimeOffset cutoffUtc,
            string selfAuthorId,
            string selfAuthorName,
            out int opponentLevel,
            out DuelOfferRejectionReason rejectionReason)
        {
            opponentLevel = 0;
            rejectionReason = ValidateMetadata(message, cutoffUtc);
            if (rejectionReason != DuelOfferRejectionReason.None)
            {
                return false;
            }

            if (IsSelf(message, selfAuthorId, selfAuthorName))
            {
                rejectionReason = DuelOfferRejectionReason.OwnMessage;
                return false;
            }

            if (message.Reactions.Any(reaction => IsBlockingReaction(reaction.Name)))
            {
                rejectionReason = DuelOfferRejectionReason.BlockingReaction;
                return false;
            }

            var match = OfferLinePattern.Matches(message.RenderedText ?? message.Text)
                .Cast<Match>()
                .FirstOrDefault(IsCfOfferLine);
            if (match == null)
            {
                rejectionReason = DuelOfferRejectionReason.NotCf;
                return false;
            }

            if (!TryParseLevel(match, out opponentLevel))
            {
                rejectionReason = DuelOfferRejectionReason.InvalidLevel;
                return false;
            }

            var eligible = (long)opponentLevel * 2 >= playerLevel && opponentLevel <= (long)playerLevel * 2;
            rejectionReason = eligible ? DuelOfferRejectionReason.None : DuelOfferRejectionReason.OutsideRewardRange;
            return eligible;
        }

        public static bool IsBlockingReaction(string name)
        {
            var normalized = (name ?? string.Empty).Trim().Trim(':').ToLowerInvariant();
            return normalized == "white_check_mark" || normalized == "✅" || normalized == "afk";
        }

        private static DuelOfferRejectionReason ValidateMetadata(
            DiscordMessageSnapshot message,
            DateTimeOffset cutoffUtc)
        {
            if (message == null || string.IsNullOrWhiteSpace(message.Id))
            {
                return DuelOfferRejectionReason.MissingMessageId;
            }

            if (string.IsNullOrWhiteSpace(message.AuthorId))
            {
                return DuelOfferRejectionReason.MissingAuthorId;
            }

            if (!message.CreatedAtUtc.HasValue)
            {
                return DuelOfferRejectionReason.MissingTimestamp;
            }

            return message.CreatedAtUtc.Value < cutoffUtc
                ? DuelOfferRejectionReason.TooOld
                : DuelOfferRejectionReason.None;
        }

        private static bool IsSelf(DiscordMessageSnapshot message, string selfAuthorId, string selfAuthorName)
        {
            return (!string.IsNullOrWhiteSpace(selfAuthorId) && message.AuthorId == selfAuthorId) ||
                   (!string.IsNullOrWhiteSpace(selfAuthorName) &&
                    string.Equals(message.Author, selfAuthorName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsCfOfferLine(Match match)
        {
            var tokens = match.Groups["tokens"].Value;
            return Regex.IsMatch(tokens, @"\bcf\b", RegexOptions.IgnoreCase) &&
                   !Regex.IsMatch(tokens, @"\bnf\b", RegexOptions.IgnoreCase);
        }

        private static bool TryParseLevel(Match match, out int level)
        {
            return int.TryParse(match.Groups["level"].Value.Replace(",", string.Empty), out level) && level > 0;
        }
    }
}
