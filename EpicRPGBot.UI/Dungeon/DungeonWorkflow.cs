using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonWorkflow
    {
        private const int BattlePollDelayMs = 1000;
        private const int BattleInactivityTimeoutMs = 60000;
        private const int DeletePromptTimeoutMs = 30000;
        private const int InviteRaceToleranceMs = 10000;
        private const long DiscordEpochMs = 1420070400000L;
        private const int DmPollDelayMs = 3000;
        private const int DmTimeoutMs = 900000;
        private const int EncounterTimeoutMs = 30000;
        private const int ResultScanCount = 20;

        private readonly IDiscordChatClient _chatClient;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly AppSettingsService _settingsService;
        private readonly Func<AppSettingsSnapshot> _getCurrentSettings;
        private readonly DungeonLobbyParser _lobbyParser;
        private readonly DungeonBattleStateParser _battleStateParser;

        public DungeonWorkflow(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender confirmedCommandSender,
            AppSettingsService settingsService,
            Func<AppSettingsSnapshot> getCurrentSettings)
            : this(
                chatClient,
                confirmedCommandSender,
                settingsService,
                getCurrentSettings,
                new DungeonLobbyParser(),
                new DungeonBattleStateParser())
        {
        }

        public DungeonWorkflow(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender confirmedCommandSender,
            AppSettingsService settingsService,
            Func<AppSettingsSnapshot> getCurrentSettings,
            DungeonLobbyParser lobbyParser,
            DungeonBattleStateParser battleStateParser)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _confirmedCommandSender = confirmedCommandSender ?? throw new ArgumentNullException(nameof(confirmedCommandSender));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _getCurrentSettings = getCurrentSettings ?? throw new ArgumentNullException(nameof(getCurrentSettings));
            _lobbyParser = lobbyParser ?? throw new ArgumentNullException(nameof(lobbyParser));
            _battleStateParser = battleStateParser ?? throw new ArgumentNullException(nameof(battleStateParser));
        }

        public async Task<DungeonRunResult> RunAsync(Action<string> report, CancellationToken cancellationToken)
        {
            try
            {
                var settings = _getCurrentSettings();
                await _chatClient.NavigateToChannelAsync(settings.ResolveDungeonListingChannelUrl());
                var playerName = await EnsureProfilePlayerNameAsync(report, cancellationToken);
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: profile name could not be resolved.");
                }

                report?.Invoke($"Profile name ready: {playerName}");
                var signupSentAtUtc = DateTimeOffset.UtcNow;
                var signupResult = await _confirmedCommandSender.SendAsync("rpg p", cancellationToken: cancellationToken);
                if (!signupResult.IsConfirmed)
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: signup 'rpg p' did not confirm.");
                }

                var dmBaselineId = await CaptureDirectMessageBaselineAsync(report, cancellationToken);
                report?.Invoke("Waiting for Army Helper DM.");
                var inviteMessage = await WaitForInviteAsync(dmBaselineId, signupSentAtUtc, report, cancellationToken);
                if (inviteMessage == null)
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: no Army Helper invite arrived within 15 minutes.");
                }

                if (!await ClickButtonByLabelAsync(inviteMessage, "Take me there", cancellationToken))
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: failed to click the latest 'Take me there' button.");
                }

                report?.Invoke("Invite accepted. Waiting for dungeon channel.");
                var partner = await WaitForPartnerAsync(playerName, report, cancellationToken);
                if (partner == null)
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: failed to resolve the dungeon partner mention.");
                }

                var partnerToken = string.IsNullOrWhiteSpace(partner.UserId)
                    ? partner.Label
                    : $"<@{partner.UserId}>";
                var entryCommand = $"rpg dung {partnerToken}";
                var entryResult = await _confirmedCommandSender.SendAsync(entryCommand, cancellationToken: cancellationToken);
                if (!entryResult.IsConfirmed)
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: 'rpg dung' did not confirm.");
                }

                report?.Invoke($"Sent {entryCommand}");
                var confirmationPrompt = await WaitForButtonPromptAsync("yes", EncounterTimeoutMs, cancellationToken);
                if (confirmationPrompt == null || !await ClickButtonByLabelAsync(confirmationPrompt, "yes", cancellationToken))
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: failed to confirm the dungeon entry prompt.");
                }

                report?.Invoke("Entry confirmed. Waiting for battle start.");
                if (!await WaitForEncounterAsync(cancellationToken))
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: encounter did not start after confirmation.");
                }

                return await RunBattleLoopAsync(report, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return DungeonRunResult.CancelledResult("Dungeon cancelled.");
            }
        }

        private async Task<string> EnsureProfilePlayerNameAsync(Action<string> report, CancellationToken cancellationToken)
        {
            var savedName = _getCurrentSettings().ProfilePlayerName.Trim();
            if (!string.IsNullOrWhiteSpace(savedName))
            {
                return savedName;
            }

            report?.Invoke("Saved profile name missing. Refreshing with 'rpg p'.");
            var result = await _confirmedCommandSender.SendAsync("rpg p", cancellationToken: cancellationToken);
            if (!result.IsConfirmed || !ProfileMessageParser.TryParsePlayerName(result.ReplyMessage?.Text, out var parsedName))
            {
                return string.Empty;
            }

            _settingsService.Save(_getCurrentSettings().WithProfilePlayerName(parsedName));
            report?.Invoke($"Saved profile name '{parsedName}'.");
            return parsedName;
        }

        private async Task<string> CaptureDirectMessageBaselineAsync(Action<string> report, CancellationToken cancellationToken)
        {
            if (!await _chatClient.OpenDirectMessageAsync("Army Helper", cancellationToken))
            {
                report?.Invoke("Army Helper DM is not visible yet. Waiting for a new invite.");
                return string.Empty;
            }

            var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
            return snapshots.LastOrDefault()?.Id ?? string.Empty;
        }

        private async Task<DiscordMessageSnapshot> WaitForInviteAsync(
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
            var invite = DungeonMessageInteraction.FindMessageAfter(snapshots, baselineMessageId, HasTakeMeThereButton);
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

        private async Task<DiscordMessageMention> WaitForPartnerAsync(
            string playerName,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < EncounterTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                var partner = _lobbyParser.FindPartnerMention(snapshots, playerName);
                if (partner != null)
                {
                    report?.Invoke($"Partner resolved as {partner.Label}.");
                    return partner;
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                waitedMs += BattlePollDelayMs;
            }

            return null;
        }

        private async Task<DiscordMessageSnapshot> WaitForButtonPromptAsync(
            string buttonLabel,
            int timeoutMs,
            CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                var prompt = DungeonMessageInteraction.FindMessageAfter(
                    snapshots,
                    string.Empty,
                    snapshot => DungeonMessageInteraction.HasButton(snapshot, buttonLabel));
                if (prompt != null)
                {
                    return prompt;
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                waitedMs += BattlePollDelayMs;
            }

            return null;
        }

        private async Task<bool> WaitForEncounterAsync(CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < EncounterTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var state = _battleStateParser.Parse(await _chatClient.GetRecentMessagesAsync(ResultScanCount), _getCurrentSettings().ProfilePlayerName);
                if (state.HasEncounter)
                {
                    return true;
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                waitedMs += BattlePollDelayMs;
            }

            return false;
        }

        private async Task<DungeonRunResult> RunBattleLoopAsync(Action<string> report, CancellationToken cancellationToken)
        {
            var lastActivitySignature = string.Empty;
            var lastActionSignature = string.Empty;
            var idleMs = 0;

            while (idleMs < BattleInactivityTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                var state = _battleStateParser.Parse(snapshots, _getCurrentSettings().ProfilePlayerName);
                if (state.HasVictory)
                {
                    await TryDeleteDungeonChannelAsync(state.DeletePrompt, report, cancellationToken);
                    return DungeonRunResult.CompletedResult("Dungeon completed.");
                }

                if (state.HasFailure)
                {
                    return DungeonRunResult.FailedResult("Dungeon stopped: battle ended in failure.");
                }

                if (!string.Equals(lastActivitySignature, state.ActivitySignature, StringComparison.Ordinal))
                {
                    lastActivitySignature = state.ActivitySignature;
                    idleMs = 0;
                }

                if (state.ShouldBite &&
                    !string.IsNullOrWhiteSpace(state.ActionSignature) &&
                    !string.Equals(lastActionSignature, state.ActionSignature, StringComparison.Ordinal))
                {
                    if (!await _chatClient.SendMessageAsync("bite", cancellationToken))
                    {
                        return DungeonRunResult.FailedResult("Dungeon stopped: failed to send 'bite'.");
                    }

                    report?.Invoke("Turn detected. Sent bite.");
                    lastActionSignature = state.ActionSignature;
                    idleMs = 0;
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                idleMs += BattlePollDelayMs;
            }

            return DungeonRunResult.FailedResult("Dungeon stopped: battle state was inactive for 60 seconds.");
        }

        private async Task TryDeleteDungeonChannelAsync(
            DiscordMessageSnapshot deletePrompt,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (!_getCurrentSettings().AutoDeleteDungeonChannel)
            {
                return;
            }

            var prompt = deletePrompt ?? await WaitForButtonPromptAsync("Delete dungeon channel", DeletePromptTimeoutMs, cancellationToken);
            if (prompt == null || !await ClickButtonByLabelAsync(prompt, "Delete dungeon channel", cancellationToken))
            {
                report?.Invoke("Auto delete skipped: delete button was not available.");
                return;
            }

            report?.Invoke("Deleted dungeon channel.");
        }

        private async Task<bool> ClickButtonByLabelAsync(
            DiscordMessageSnapshot snapshot,
            string buttonLabel,
            CancellationToken cancellationToken)
        {
            if (snapshot?.Buttons == null)
            {
                return false;
            }

            var button = snapshot.Buttons.FirstOrDefault(candidate => DungeonMessageInteraction.LabelsMatch(candidate.Label, buttonLabel));
            return button != null &&
                   await _chatClient.ClickMessageButtonAsync(snapshot.Id, button.RowIndex, button.ColumnIndex, cancellationToken);
        }

        private static bool HasTakeMeThereButton(DiscordMessageSnapshot snapshot)
        {
            return DungeonMessageInteraction.HasButton(snapshot, "Take me there");
        }
    }
}
