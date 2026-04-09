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
        private const int EncounterTimeoutMs = 30000;
        private const int EntryRetryDelayMs = 5000;
        private const int PartnerBusyRetryCount = 2;
        private const int ResultScanCount = 20;

        private readonly IDiscordChatClient _chatClient;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly AppSettingsService _settingsService;
        private readonly Func<AppSettingsSnapshot> _getCurrentSettings;
        private readonly DungeonLobbyParser _lobbyParser;
        private readonly DungeonBattleStateParser _battleStateParser;
        private readonly DungeonInviteWatcher _inviteWatcher;
        private readonly DungeonEntryReplyParser _entryReplyParser;
        private readonly Func<int, CancellationToken, Task> _waitAsync;

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
                new DungeonBattleStateParser(),
                new DungeonInviteWatcher(chatClient),
                new DungeonEntryReplyParser(),
                Task.Delay)
        {
        }

        public DungeonWorkflow(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender confirmedCommandSender,
            AppSettingsService settingsService,
            Func<AppSettingsSnapshot> getCurrentSettings,
            DungeonLobbyParser lobbyParser,
            DungeonBattleStateParser battleStateParser)
            : this(
                chatClient,
                confirmedCommandSender,
                settingsService,
                getCurrentSettings,
                lobbyParser,
                battleStateParser,
                new DungeonInviteWatcher(chatClient),
                new DungeonEntryReplyParser(),
                Task.Delay)
        {
        }

        public DungeonWorkflow(
            IDiscordChatClient chatClient,
            ConfirmedCommandSender confirmedCommandSender,
            AppSettingsService settingsService,
            Func<AppSettingsSnapshot> getCurrentSettings,
            DungeonLobbyParser lobbyParser,
            DungeonBattleStateParser battleStateParser,
            DungeonInviteWatcher inviteWatcher,
            DungeonEntryReplyParser entryReplyParser,
            Func<int, CancellationToken, Task> waitAsync)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _confirmedCommandSender = confirmedCommandSender ?? throw new ArgumentNullException(nameof(confirmedCommandSender));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _getCurrentSettings = getCurrentSettings ?? throw new ArgumentNullException(nameof(getCurrentSettings));
            _lobbyParser = lobbyParser ?? throw new ArgumentNullException(nameof(lobbyParser));
            _battleStateParser = battleStateParser ?? throw new ArgumentNullException(nameof(battleStateParser));
            _inviteWatcher = inviteWatcher ?? throw new ArgumentNullException(nameof(inviteWatcher));
            _entryReplyParser = entryReplyParser ?? throw new ArgumentNullException(nameof(entryReplyParser));
            _waitAsync = waitAsync ?? throw new ArgumentNullException(nameof(waitAsync));
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

                var dmBaselineId = await _inviteWatcher.CaptureDirectMessageBaselineAsync(report, cancellationToken);
                report?.Invoke("Waiting for Army Helper DM.");
                while (true)
                {
                    var inviteMessage = await _inviteWatcher.WaitForInviteAsync(
                        dmBaselineId,
                        signupSentAtUtc,
                        report,
                        cancellationToken);
                    if (inviteMessage == null)
                    {
                        return DungeonRunResult.FailedResult("Dungeon stopped: no Army Helper invite arrived within 15 minutes.");
                    }

                    dmBaselineId = inviteMessage.Id;
                    if (!await ClickButtonByLabelAsync(inviteMessage, "Take me there", cancellationToken))
                    {
                        return DungeonRunResult.FailedResult("Dungeon stopped: failed to click the latest 'Take me there' button.");
                    }

                    report?.Invoke("Invite accepted. Waiting for dungeon channel.");
                    var entryPreparation = await WaitForEntryPreparationAsync(playerName, report, cancellationToken);
                    if (entryPreparation == null)
                    {
                        return DungeonRunResult.FailedResult("Dungeon stopped: failed to resolve the dungeon partner mention or entry prompt.");
                    }

                    DiscordMessageSnapshot confirmationPrompt = null;
                    if (entryPreparation.HasConfirmationPrompt)
                    {
                        report?.Invoke("Partner entry prompt detected. Accepting the dungeon without sending a new invite.");
                        confirmationPrompt = entryPreparation.ConfirmationPrompt;
                    }
                    else
                    {
                        var entryResult = await TryEnterDungeonAsync(entryPreparation.Partner, report, cancellationToken);
                        if (entryResult == DungeonEntryAttemptResult.WaitForNextInvite)
                        {
                            report?.Invoke("Partner is busy. Waiting for a fresh dungeon invite.");
                            continue;
                        }

                        if (entryResult == DungeonEntryAttemptResult.Failed)
                        {
                            return DungeonRunResult.FailedResult("Dungeon stopped: 'rpg dung' did not confirm.");
                        }
                    }

                    confirmationPrompt ??= await WaitForButtonPromptAsync("yes", EncounterTimeoutMs, cancellationToken);
                    if (confirmationPrompt == null ||
                        !await ClickButtonByLabelAsync(confirmationPrompt, "yes", cancellationToken))
                    {
                        return DungeonRunResult.FailedResult("Dungeon stopped: failed to confirm the dungeon entry prompt.");
                    }

                    break;
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

        private async Task<DungeonEntryPreparation> WaitForEntryPreparationAsync(
            string playerName,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < EncounterTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                var confirmationPrompt = FindEntryConfirmationPrompt(snapshots);
                if (confirmationPrompt != null)
                {
                    report?.Invoke("Existing dungeon entry prompt detected.");
                    return DungeonEntryPreparation.ForConfirmationPrompt(confirmationPrompt);
                }

                var partner = _lobbyParser.FindPartnerMention(snapshots, playerName);
                if (partner != null)
                {
                    report?.Invoke($"Partner resolved as {partner.Label}.");
                    return DungeonEntryPreparation.ForPartner(partner);
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                waitedMs += BattlePollDelayMs;
            }

            return null;
        }

        private static DiscordMessageSnapshot FindEntryConfirmationPrompt(
            IReadOnlyList<DiscordMessageSnapshot> snapshots)
        {
            return DungeonMessageInteraction.FindMessageAfter(
                snapshots,
                string.Empty,
                snapshot => DungeonMessageInteraction.HasButton(snapshot, "yes") &&
                            (snapshot.RenderedText?.IndexOf("ARE YOU SURE YOU WANT TO ENTER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             snapshot.Text?.IndexOf("ARE YOU SURE YOU WANT TO ENTER", StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private async Task<DungeonEntryAttemptResult> TryEnterDungeonAsync(
            DiscordMessageMention partner,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var partnerToken = string.IsNullOrWhiteSpace(partner.UserId)
                ? partner.Label
                : $"<@{partner.UserId}>";
            var entryCommand = $"rpg dung {partnerToken}";
            var maxAttempts = PartnerBusyRetryCount + 1;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var entryResult = await _confirmedCommandSender.SendAsync(entryCommand, cancellationToken: cancellationToken);
                if (!entryResult.IsConfirmed)
                {
                    return DungeonEntryAttemptResult.Failed;
                }

                if (_entryReplyParser.Parse(entryResult.ReplyMessage.Text) != DungeonEntryReplyKind.PartnerBusy)
                {
                    report?.Invoke($"Sent {entryCommand}");
                    return DungeonEntryAttemptResult.Confirmed;
                }

                if (attempt >= maxAttempts)
                {
                    return DungeonEntryAttemptResult.WaitForNextInvite;
                }

                report?.Invoke(
                    $"Partner is in the middle of a command. Waiting 5 seconds before retry {attempt + 1}/{maxAttempts}.");
                await _waitAsync(EntryRetryDelayMs, cancellationToken);
            }

            return DungeonEntryAttemptResult.Failed;
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
        private enum DungeonEntryAttemptResult
        {
            Failed = 0,
            Confirmed = 1,
            WaitForNextInvite = 2
        }
    }
}
