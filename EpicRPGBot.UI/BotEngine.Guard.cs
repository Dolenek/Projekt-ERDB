using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private const int ProcessedGuardMessageLimit = 16;

        private readonly HashSet<string> _processedGuardMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedGuardMessageOrder = new Queue<string>();
        private string _activeGuardMessageId = string.Empty;

        private string ResolveGuardTargetMessageId(bool latestHasGuard, bool previousHasGuard)
        {
            if (latestHasGuard && !string.IsNullOrWhiteSpace(_lastMessageId))
            {
                return _lastMessageId;
            }

            if (previousHasGuard && !string.IsNullOrWhiteSpace(_previousMessageId))
            {
                return _previousMessageId;
            }

            return string.Empty;
        }

        private bool TryBeginGuardSolve(string targetMessageId)
        {
            if (string.IsNullOrWhiteSpace(targetMessageId))
            {
                ReportSolverInfo("Guard detected, but no target message id was available.");
                return false;
            }

            if (string.Equals(targetMessageId, _activeGuardMessageId, StringComparison.Ordinal))
            {
                ReportSolverInfo($"Guard message {targetMessageId} is already being solved; duplicate trigger ignored.");
                return false;
            }

            if (_processedGuardMessageIds.Contains(targetMessageId))
            {
                ReportSolverInfo($"Guard message {targetMessageId} was already handled; duplicate trigger ignored.");
                return false;
            }

            if (_captchaSolver.IsBusy)
            {
                ReportSolverInfo($"Guard solver is already busy; skipped new solve for message {targetMessageId}.");
                return false;
            }

            _activeGuardMessageId = targetMessageId;
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
            _processedGuardMessageIds.Clear();
            _processedGuardMessageOrder.Clear();
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
