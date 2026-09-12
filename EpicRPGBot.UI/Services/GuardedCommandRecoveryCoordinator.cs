#nullable enable
using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal sealed class GuardedCommandRecoveryCoordinator
    {
        private readonly object _sync = new object();
        private GuardedCommandRecoveryRegistration? _currentIncident;

        public GuardedCommandRecoveryRegistration ObserveIncident(
            string incidentMessageId,
            string interruptedCommand)
        {
            lock (_sync)
            {
                var incident = _currentIncident;
                if (incident == null ||
                    (incident.IsCompleted &&
                     !string.Equals(incident.IncidentMessageId, incidentMessageId, StringComparison.Ordinal)))
                {
                    incident = new GuardedCommandRecoveryRegistration(incidentMessageId);
                    _currentIncident = incident;
                }

                incident.AttachInterruptedCommand(interruptedCommand);
                return incident;
            }
        }

        public string InterruptedCommand
        {
            get
            {
                lock (_sync)
                {
                    return _currentIncident == null || _currentIncident.IsCompleted
                        ? string.Empty
                        : _currentIncident.InterruptedCommand;
                }
            }
        }

        public string CompleteIncident(DiscordMessageSnapshot? continuationReply = null)
        {
            lock (_sync)
            {
                if (_currentIncident == null || _currentIncident.IsCompleted)
                {
                    return string.Empty;
                }

                var interruptedCommand = _currentIncident.InterruptedCommand;
                _currentIncident.Complete(
                    new GuardedCommandRecoveryCompletion(true, continuationReply));
                return interruptedCommand;
            }
        }

        public GuardedCommandRecoveryRegistration? FindIncidentForCommand(string command)
        {
            lock (_sync)
            {
                if (_currentIncident == null || string.IsNullOrWhiteSpace(command))
                {
                    return null;
                }

                return string.Equals(
                    _currentIncident.InterruptedCommand,
                    command,
                    StringComparison.OrdinalIgnoreCase)
                    ? _currentIncident
                    : null;
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                _currentIncident?.Complete(new GuardedCommandRecoveryCompletion(false, null));
                _currentIncident = null;
            }
        }

        public async Task<ConfirmedCommandSendResult> ExecuteAsync(
            string command,
            Func<Action<GuardedCommandRecoveryRegistration>, Task<ConfirmedCommandSendResult>> sendAttempt,
            Action<string>? reportRecovery,
            Action? recoveryFinished)
        {
            var waitedForGuard = false;
            try
            {
                while (true)
                {
                    GuardedCommandRecoveryRegistration? registration = null;
                    var result = await sendAttempt(detectedRecovery => registration = detectedRecovery);
                    if (registration == null)
                    {
                        return result;
                    }

                    waitedForGuard = true;
                    var completion = await registration.Completion;
                    ReleaseIncident(registration);
                    var completedResult = ResolveCompletion(
                        command,
                        result,
                        completion,
                        reportRecovery);
                    if (completedResult != null)
                    {
                        return completedResult;
                    }
                }
            }
            finally
            {
                if (waitedForGuard)
                {
                    recoveryFinished?.Invoke();
                }
            }
        }

        private static ConfirmedCommandSendResult? ResolveCompletion(
            string command,
            ConfirmedCommandSendResult guardResult,
            GuardedCommandRecoveryCompletion completion,
            Action<string>? reportRecovery)
        {
            if (!completion.WasCleared)
            {
                return new ConfirmedCommandSendResult(
                    guardResult.OutgoingMessage,
                    null!,
                    guardResult.AttemptCount);
            }

            if (completion.ContinuationReply != null)
            {
                reportRecovery?.Invoke(
                    $"Guard clear completed interrupted '{command}' without a retry.");
                return new ConfirmedCommandSendResult(
                    guardResult.OutgoingMessage,
                    completion.ContinuationReply,
                    guardResult.AttemptCount);
            }

            reportRecovery?.Invoke($"Retrying interrupted '{command}' after EPIC GUARD clear.");
            return null;
        }

        private void ReleaseIncident(GuardedCommandRecoveryRegistration registration)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_currentIncident, registration))
                {
                    _currentIncident = null;
                }
            }
        }
    }

    internal sealed class GuardedCommandRecoveryRegistration
    {
        private readonly TaskCompletionSource<GuardedCommandRecoveryCompletion> _completion =
            new TaskCompletionSource<GuardedCommandRecoveryCompletion>(
                TaskCreationOptions.RunContinuationsAsynchronously);

        public GuardedCommandRecoveryRegistration(string incidentMessageId)
        {
            IncidentMessageId = incidentMessageId ?? string.Empty;
        }

        public string IncidentMessageId { get; }
        public string InterruptedCommand { get; private set; } = string.Empty;
        public Task<GuardedCommandRecoveryCompletion> Completion => _completion.Task;
        public bool IsCompleted => _completion.Task.IsCompleted;

        public void AttachInterruptedCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(InterruptedCommand) && !string.IsNullOrWhiteSpace(command))
            {
                InterruptedCommand = command;
            }
        }

        public void Complete(GuardedCommandRecoveryCompletion completion)
        {
            _completion.TrySetResult(completion);
        }
    }

    internal sealed class GuardedCommandRecoveryCompletion
    {
        public GuardedCommandRecoveryCompletion(
            bool wasCleared,
            DiscordMessageSnapshot? continuationReply)
        {
            WasCleared = wasCleared;
            ContinuationReply = continuationReply;
        }

        public bool WasCleared { get; }
        public DiscordMessageSnapshot? ContinuationReply { get; }
    }
}
