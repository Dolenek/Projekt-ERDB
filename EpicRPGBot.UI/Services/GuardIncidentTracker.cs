using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class GuardIncidentTracker
    {
        private static readonly TimeSpan ReminderInterval = TimeSpan.FromSeconds(10);
        private DateTime _lastAlertUtc = DateTime.MinValue;
        private bool _isActive;

        public bool IsActive => _isActive;

        public static bool ContainsGuardPrompt(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.IndexOf("EPIC GUARD: stop there,", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("Select the item of the image above or respond with the item name", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool ContainsGuardClear(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.IndexOf("EPIC GUARD: Everything seems fine", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   message.IndexOf("keep playing", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public GuardAlertNotification RegisterDetection(string detectionInfo)
        {
            var now = DateTime.UtcNow;
            if (!_isActive)
            {
                _isActive = true;
                _lastAlertUtc = now;
                return new GuardAlertNotification(GuardAlertKind.FirstDetected, detectionInfo);
            }

            if (now - _lastAlertUtc < ReminderInterval)
            {
                return null;
            }

            _lastAlertUtc = now;
            return new GuardAlertNotification(GuardAlertKind.Reminder, "EPIC GUARD is still active.");
        }

        public GuardAlertNotification ClearIfActive()
        {
            if (!_isActive)
            {
                return null;
            }

            Reset();
            return new GuardAlertNotification(GuardAlertKind.Cleared, "EPIC GUARD cleared; everything seems fine.");
        }

        public void Reset()
        {
            _isActive = false;
            _lastAlertUtc = DateTime.MinValue;
        }
    }
}
