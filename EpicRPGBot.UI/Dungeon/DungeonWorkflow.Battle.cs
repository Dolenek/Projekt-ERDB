using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed partial class DungeonWorkflow
    {
        private async Task<bool> WaitForEncounterAsync(CancellationToken cancellationToken)
        {
            var waitedMs = 0;
            while (waitedMs < EncounterTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                var state = _battleStateParser.Parse(snapshots, _getCurrentSettings().ProfilePlayerName);
                if (state.HasEncounter)
                {
                    return true;
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                waitedMs += BattlePollDelayMs;
            }

            return false;
        }

        private async Task<DungeonRunResult> RunBattleLoopAsync(
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var lastActivitySignature = string.Empty;
            var turnActionGate = new DungeonTurnActionGate();
            var idleMs = 0;

            while (idleMs < BattleInactivityTimeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var snapshots = await _chatClient.GetRecentMessagesAsync(ResultScanCount);
                var state = _battleStateParser.Parse(snapshots, _getCurrentSettings().ProfilePlayerName);
                var terminalResult = await TryResolveTerminalStateAsync(state, report, cancellationToken);
                if (terminalResult != null)
                {
                    return terminalResult;
                }

                if (!string.Equals(lastActivitySignature, state.ActivitySignature, StringComparison.Ordinal))
                {
                    lastActivitySignature = state.ActivitySignature;
                    idleMs = 0;
                }

                if (turnActionGate.ShouldSendBite(state.ShouldBite))
                {
                    if (!await _chatClient.SendMessageAsync("bite", cancellationToken))
                    {
                        return DungeonRunResult.FailedResult("Dungeon stopped: failed to send 'bite'.");
                    }

                    report?.Invoke("Turn detected. Sent bite.");
                    idleMs = 0;
                }

                await Task.Delay(BattlePollDelayMs, cancellationToken);
                idleMs += BattlePollDelayMs;
            }

            return DungeonRunResult.FailedResult("Dungeon stopped: battle state was inactive for 60 seconds.");
        }

        private async Task<DungeonRunResult> TryResolveTerminalStateAsync(
            DungeonBattleState state,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (state.HasVictory)
            {
                await TryDeleteDungeonChannelAsync(state.DeletePrompt, report, cancellationToken);
                return DungeonRunResult.CompletedResult("Dungeon completed.");
            }

            return state.HasFailure
                ? DungeonRunResult.FailedResult("Dungeon stopped: battle ended in failure.")
                : null;
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

            var prompt = deletePrompt ?? await _entryPromptWatcher.WaitForButtonPromptAsync(
                "Delete dungeon channel",
                ResultScanCount,
                DeletePromptTimeoutMs,
                BattlePollDelayMs,
                cancellationToken);
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

            var button = snapshot.Buttons.FirstOrDefault(candidate =>
                DungeonMessageInteraction.LabelsMatch(candidate.Label, buttonLabel));
            return button != null &&
                   await _chatClient.ClickMessageButtonAsync(
                       snapshot.Id,
                       button.RowIndex,
                       button.ColumnIndex,
                       cancellationToken);
        }
    }
}
