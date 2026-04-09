using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonInviteWatcher
    {
        private const long DiscordEpochMs = 1420070400000L;
        private const int DmPollDelayMs = 3000;
        private const int DmTimeoutMs = 900000;
        private const int InviteRaceToleranceMs = 10000;
        private const int ResultScanCount = 20;

        private readonly IDiscordChatClient _chatClient;

        public DungeonInviteWatcher(IDiscordChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public async Task<string> CaptureDirectMessageBaselineAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (!await _chatClient.OpenDirectMessageAsync("Army Helper", cancellationToken))
            {
                report?.Invoke("Army Helper DM is not visible yet. Waiting for a new invite.");
                return string.Empty;
            }

            var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
            return snapshots.LastOrDefault()?.Id ?? string.Empty;
        }

        public async Task<DiscordMessageSnapshot> WaitForInviteAsync(
            string baselineMessageId,
            DateTimeOffset signupSentAtUtc,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            var baselineId = baselineMessageId ?? string.Empty;
            var dmOpened = false;
            while (waitedMs < DmTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!dmOpened)
                {
                    dmOpened = await _chatClient.OpenDirectMessageAsync("Army Helper", cancellationToken);
                    if (!dmOpened)
                    {
                        report?.Invoke("Army Helper DM still unavailable.");
                        await Task.Delay(DmPollDelayMs, cancellationToken);
                        waitedMs += DmPollDelayMs;
                        continue;
                    }
                }

                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                if (string.IsNullOrWhiteSpace(baselineId))
                {
                    baselineId = snapshots.LastOrDefault()?.Id ?? string.Empty;
                }

                var invite = ResolveInviteMessage(snapshots, baselineId, signupSentAtUtc);
                if (invite != null)
                {
                    return invite;
                }

                await Task.Delay(DmPollDelayMs, cancellationToken);
                waitedMs += DmPollDelayMs;
            }

            return null;
        }

        private static DiscordMessageSnapshot ResolveInviteMessage(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string baselineMessageId,
            DateTimeOffset signupSentAtUtc)
        {
            var invite = DungeonMessageInteraction.FindMessageAfter(
                snapshots,
                baselineMessageId,
                HasTakeMeThereButton);
            if (invite != null)
            {
                return invite;
            }

            var latestVisibleInvite = snapshots?.LastOrDefault(HasTakeMeThereButton);
            var inviteCreatedAtUtc = TryParseSnowflakeTimestampUtc(latestVisibleInvite?.Id);
            return inviteCreatedAtUtc.HasValue &&
                   inviteCreatedAtUtc.Value >= signupSentAtUtc.AddMilliseconds(-InviteRaceToleranceMs)
                ? latestVisibleInvite
                : null;
        }

        private static DateTimeOffset? TryParseSnowflakeTimestampUtc(string messageId)
        {
            var candidate = ExtractSnowflakeToken(messageId);
            if (!ulong.TryParse(candidate, out var snowflake))
            {
                return null;
            }

            var unixTimeMs = (long)(snowflake >> 22) + DiscordEpochMs;
            return DateTimeOffset.FromUnixTimeMilliseconds(unixTimeMs);
        }

        private static string ExtractSnowflakeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var trimmed = value.Trim();
            var dashIndex = trimmed.LastIndexOf('-');
            return dashIndex >= 0 && dashIndex + 1 < trimmed.Length
                ? trimmed.Substring(dashIndex + 1)
                : trimmed;
        }

        private static bool HasTakeMeThereButton(DiscordMessageSnapshot snapshot)
        {
            return DungeonMessageInteraction.HasButton(snapshot, "Take me there");
        }
    }
}
