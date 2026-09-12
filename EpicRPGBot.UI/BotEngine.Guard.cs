using System;
using System.Collections.Generic;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private const int ProcessedGuardMessageLimit = 16;

        private readonly HashSet<string> _processedGuardMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedGuardMessageOrder = new Queue<string>();
        private string _activeGuardMessageId = string.Empty;
        private DiscordMessageReference _guardMessageReference;
        private bool _guardSolveStartedForIncident;

        private bool IsGuardSolveActive => !string.IsNullOrWhiteSpace(_activeGuardMessageId);
        private bool IsGuardIncidentActive => _guardIncidentTracker.IsActive || _guardSolveStartedForIncident || IsGuardSolveActive;

        private DiscordMessageSnapshot ResolveGuardTargetSnapshot(bool latestHasGuard, bool previousHasGuard)
        {
            if (latestHasGuard && _lastMessageSnapshot != null)
            {
                return _lastMessageSnapshot;
            }

            if (previousHasGuard && _previousMessageSnapshot != null)
            {
                return _previousMessageSnapshot;
            }

            return null;
        }

        private bool TryBeginGuardSolve(DiscordMessageSnapshot targetSnapshot)
        {
            var targetMessageId = targetSnapshot?.Id ?? string.Empty;
            var targetReference = DiscordMessageReference.FromSnapshot(targetSnapshot);
            if (string.IsNullOrWhiteSpace(targetMessageId))
            {
                ReportSolverInfo("Guard detected, but no target message id was available.", targetReference);
                return false;
            }

            if (string.Equals(targetMessageId, _activeGuardMessageId, StringComparison.Ordinal))
            {
                ReportSolverInfo($"Guard message {targetMessageId} is already being solved; duplicate trigger ignored.", targetReference);
                return false;
            }

            if (_guardSolveStartedForIncident)
            {
                ReportSolverInfo($"Guard incident already has a solve attempt; ignored additional prompt message {targetMessageId}.", targetReference);
                return false;
            }

            if (_processedGuardMessageIds.Contains(targetMessageId))
            {
                ReportSolverInfo($"Guard message {targetMessageId} was already handled; duplicate trigger ignored.", targetReference);
                return false;
            }

            if (_puzzleSolver.IsBusy)
            {
                ReportSolverInfo($"Guard solver is already busy; skipped new solve for message {targetMessageId}.", targetReference);
                return false;
            }

            _activeGuardMessageId = targetMessageId;
            _guardMessageReference = targetReference;
            _guardSolveStartedForIncident = true;
            RememberProcessedGuardMessage(targetMessageId);
            return true;
        }

        private void CompleteGuardSolve(string targetMessageId)
        {
            if (string.Equals(_activeGuardMessageId, targetMessageId, StringComparison.Ordinal))
            {
                _activeGuardMessageId = string.Empty;
            }
        }

        private void ResetGuardMessageTracking()
        {
            _activeGuardMessageId = string.Empty;
            _guardMessageReference = null;
            _guardSolveStartedForIncident = false;
            _processedGuardMessageIds.Clear();
            _processedGuardMessageOrder.Clear();
        }

        private bool ShouldBlockOutgoingForGuard(bool allowDuringGuard)
        {
            return IsGuardIncidentActive && !allowDuringGuard;
        }

        private bool ReportBlockedOutgoingForGuard(string text, bool allowDuringGuard)
        {
            if (!ShouldBlockOutgoingForGuard(allowDuringGuard))
            {
                return false;
            }

            ReportSolverInfo($"Blocked '{text}' while EPIC GUARD incident is active.");
            return true;
        }

        private void RememberProcessedGuardMessage(string messageId)
        {
            if (!_processedGuardMessageIds.Add(messageId))
            {
                return;
            }

            _processedGuardMessageOrder.Enqueue(messageId);
            while (_processedGuardMessageOrder.Count > ProcessedGuardMessageLimit)
            {
                _processedGuardMessageIds.Remove(_processedGuardMessageOrder.Dequeue());
            }
        }
    }
}
