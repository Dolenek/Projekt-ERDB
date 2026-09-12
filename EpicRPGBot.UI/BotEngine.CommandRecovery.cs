using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
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
            CompleteInterruptedCommand(snapshot, currentReference);
            OnGuardNotification?.Invoke(cleared);
            ReportSolverInfo(cleared.Message, currentReference);
            return true;
        }

        private void CompleteInterruptedCommand(
            DiscordMessageSnapshot clearReply,
            DiscordMessageReference currentReference)
        {
            var interruptedCommand = _guardedCommandRecovery.InterruptedCommand;
            var continuationReply = GuardedCommandContinuationClassifier.IsInlineContinuation(
                interruptedCommand,
                clearReply)
                ? clearReply
                : null;
            interruptedCommand = _guardedCommandRecovery.CompleteIncident(continuationReply);
            if (string.IsNullOrWhiteSpace(interruptedCommand))
            {
                ResumeUnknownGuardIncident(currentReference);
                return;
            }

            if (continuationReply != null)
            {
                ReportSolverInfo(
                    $"Guard clear included the result for interrupted '{interruptedCommand}'.",
                    currentReference);
                return;
            }

            ReportSolverInfo(
                $"Guard clear will resume interrupted '{interruptedCommand}'.",
                currentReference);
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
