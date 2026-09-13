using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private const int GuardContinuationPollDelayMs = 250;
        private const int GuardContinuationTimeoutMs = 10000;
        private const int GuardContinuationScanCount = 20;

        private readonly GuardedCommandRecoveryCoordinator _guardedCommandRecovery =
            new GuardedCommandRecoveryCoordinator();
        private string _activeConfirmedCommand = string.Empty;

        private async Task<ConfirmedCommandSendResult> SendConfirmedCommandWithGuardRecoveryAsync(
            string text,
            Action<DiscordMessageSnapshot> onOutgoingRegistered,
            Action<DiscordMessageSnapshot> onOutgoingSnapshotRegistered = null,
            bool allowDuringGuard = false,
            bool allowDuringInteractivePrompt = false)
        {
            var result = await _guardedCommandRecovery.ExecuteAsync(
                text,
                guardDetected => SendConfirmedCommandWithGlobalCooldownCoreAsync(
                    text,
                    onOutgoingRegistered,
                    onOutgoingSnapshotRegistered,
                    allowDuringGuard,
                    allowDuringInteractivePrompt,
                    guardDetected),
                info => ReportSolverInfo(info),
                () => _scheduler.ResumeAll(_running));
            if (result.IsConfirmed)
            {
                OnCommandConfirmed?.Invoke(text, result.ReplyMessage);
            }

            return result;
        }

        private GuardedCommandRecoveryRegistration ObserveGuardedCommand(
            DiscordMessageSnapshot guardReply,
            string command)
        {
            return _guardedCommandRecovery.ObserveIncident(
                guardReply?.Id ?? string.Empty,
                command);
        }

        private async Task<bool> TryHandleGuardedCommandResultAsync(
            ConfirmedCommandSendResult result,
            string command,
            Action<GuardedCommandRecoveryRegistration> onGuardDetected)
        {
            var recovery = IsBlockingGuardReply(result.ReplyMessage)
                ? ObserveGuardedCommand(result.ReplyMessage, command)
                : _guardedCommandRecovery.FindIncidentForCommand(command);
            if (recovery == null)
            {
                return false;
            }

            onGuardDetected?.Invoke(recovery);
            ProcessObservedSnapshot(result.ReplyMessage, true);
            await ProcessIncomingMessagesAsync();
            return true;
        }

        private void ObserveGuardIncident(string incidentMessageId)
        {
            _guardedCommandRecovery.ObserveIncident(incidentMessageId, _activeConfirmedCommand);
        }

        private bool ShouldApplySnapshotToScheduler(DiscordMessageSnapshot snapshot)
        {
            var message = snapshot?.Text ?? string.Empty;
            if (!GuardIncidentTracker.ContainsGuardClear(message))
            {
                return true;
            }

            return GuardedCommandContinuationClassifier.IsInlineContinuation(
                _guardedCommandRecovery.InterruptedCommand,
                snapshot);
        }

        private bool ShouldDeferGuardContinuation(DiscordMessageSnapshot snapshot)
        {
            var interruptedCommand = _guardedCommandRecovery.InterruptedCommand;
            return !string.IsNullOrWhiteSpace(interruptedCommand) &&
                GuardIncidentTracker.ContainsGuardClear(snapshot?.Text ?? string.Empty) &&
                !GuardedCommandContinuationClassifier.IsInlineContinuation(
                    interruptedCommand,
                    snapshot);
        }

        private bool TryCompleteGuardIncident(
            DiscordMessageSnapshot snapshot,
            DiscordMessageReference currentReference)
        {
            if (!GuardIncidentTracker.ContainsGuardClear(snapshot?.Text ?? string.Empty))
            {
                return false;
            }

            var cleared = _guardIncidentTracker.ClearIfActive(currentReference);
            if (cleared == null)
            {
                return true;
            }

            _puzzleSolver.CancelCurrentSolve();
            CompleteGuardSolve(_activeGuardMessageId);
            ResetGuardMessageTracking();
            BeginInterruptedCommandCompletion(snapshot, currentReference);
            OnGuardNotification?.Invoke(cleared);
            ReportSolverInfo(cleared.Message, currentReference);
            return true;
        }

        private void BeginInterruptedCommandCompletion(
            DiscordMessageSnapshot clearReply,
            DiscordMessageReference currentReference)
        {
            var interruptedCommand = _guardedCommandRecovery.InterruptedCommand;
            if (string.IsNullOrWhiteSpace(interruptedCommand))
            {
                _guardedCommandRecovery.CompleteIncident(clearReply);
                ResumeUnknownGuardIncident(currentReference);
                return;
            }

            if (GuardedCommandContinuationClassifier.IsInlineContinuation(
                interruptedCommand,
                clearReply))
            {
                _guardedCommandRecovery.CompleteIncident(clearReply);
                ReportSolverInfo(
                    $"Guard clear included the result for interrupted '{interruptedCommand}'.",
                    currentReference);
                return;
            }

            ReportSolverInfo(
                $"Waiting for '{interruptedCommand}' result after guard clear; the command will not be resent.",
                currentReference);
            _ = CompleteDeferredGuardContinuationAsync(
                interruptedCommand,
                clearReply,
                currentReference);
        }

        private async Task CompleteDeferredGuardContinuationAsync(
            string interruptedCommand,
            DiscordMessageSnapshot clearReply,
            DiscordMessageReference currentReference)
        {
            DiscordMessageSnapshot continuationReply;
            try
            {
                continuationReply = await WaitForGuardContinuationAsync(
                    interruptedCommand,
                    clearReply,
                    _stopCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                ReportSolverInfo(
                    $"Guard result refresh failed: {ex.Message}. Continuing without resending.",
                    currentReference);
                continuationReply = clearReply;
            }

            CompleteDeferredGuardContinuation(continuationReply, currentReference);
        }

        private async Task<DiscordMessageSnapshot> WaitForGuardContinuationAsync(
            string interruptedCommand,
            DiscordMessageSnapshot clearReply,
            System.Threading.CancellationToken cancellationToken)
        {
            var latestClearReply = clearReply;
            for (var waitedMs = 0; waitedMs < GuardContinuationTimeoutMs; waitedMs += GuardContinuationPollDelayMs)
            {
                await SafeDelay(GuardContinuationPollDelayMs, cancellationToken);
                var snapshots = await _chatClient.GetRecentMessagesAsync(GuardContinuationScanCount);
                latestClearReply = FindSnapshot(snapshots, clearReply.Id) ?? latestClearReply;
                var continuation = GuardedCommandContinuationClassifier.FindContinuation(
                    interruptedCommand,
                    clearReply.Id,
                    snapshots);
                if (continuation != null)
                {
                    return continuation;
                }

                if (HasSnapshotContentChanged(clearReply, latestClearReply))
                {
                    return latestClearReply;
                }
            }

            return latestClearReply;
        }

        private static bool HasSnapshotContentChanged(
            DiscordMessageSnapshot original,
            DiscordMessageSnapshot current)
        {
            if (original == null || current == null)
            {
                return false;
            }

            return !string.Equals(original.Text, current.Text, StringComparison.Ordinal) ||
                !string.Equals(original.RenderedText, current.RenderedText, StringComparison.Ordinal);
        }

        private void CompleteDeferredGuardContinuation(
            DiscordMessageSnapshot continuationReply,
            DiscordMessageReference currentReference)
        {
            OnMessageSeen?.Invoke(continuationReply);
            _scheduler.HandleResponse(continuationReply, _running);
            var trainingHandled = TryHandleTrainingPrompt(continuationReply);
            var interruptedCommand = _guardedCommandRecovery.CompleteIncident(continuationReply);
            if (string.IsNullOrWhiteSpace(interruptedCommand))
            {
                return;
            }

            if (trainingHandled)
            {
                _previousMessageText = continuationReply.Text ?? string.Empty;
            }
            else
            {
                EventCheck(continuationReply);
            }

            ReportSolverInfo(
                $"Continued interrupted '{interruptedCommand}' from its guard result without resending.",
                currentReference);
        }

        private static DiscordMessageSnapshot FindSnapshot(
            System.Collections.Generic.IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string messageId)
        {
            if (snapshots == null)
            {
                return null;
            }

            for (var index = 0; index < snapshots.Count; index++)
            {
                if (string.Equals(snapshots[index]?.Id, messageId, StringComparison.Ordinal))
                {
                    return snapshots[index];
                }
            }

            return null;
        }

        private void ResumeUnknownGuardIncident(DiscordMessageReference currentReference)
        {
            _scheduler.ResumeAll(_running);
            if (QueueCooldownSnapshotRequest())
            {
                ReportSolverInfo(
                    "Queued 'rpg cd' after guard clear to resync scheduling.",
                    currentReference);
            }
        }

        private static bool IsBlockingGuardReply(DiscordMessageSnapshot reply)
        {
            var message = reply?.Text ?? string.Empty;
            return GuardIncidentTracker.ContainsGuardPrompt(message) &&
                !GuardIncidentTracker.ContainsGuardClear(message);
        }
    }
}
