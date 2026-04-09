using System;
using System.Windows;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const int ProcessedMessageLimit = 64;

        private void OnPolledMessage(DiscordMessageSnapshot snapshot)
        {
            UiDispatcher.OnUI(() => HandleObservedMessage(snapshot));
        }

        private void HandleObservedMessage(DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null ||
                string.IsNullOrWhiteSpace(snapshot.Id) ||
                !TryRememberProcessedMessage(snapshot.Id))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(snapshot.Text))
            {
                _last.Add(snapshot.Text);
            }

            if (TryStopEngineForPreviousCommandBusy(snapshot))
            {
                return;
            }

            var trackerUpdated = _cooldownTracker.ApplyMessage(snapshot.Text);
            if (trackerUpdated)
            {
                SyncEngineFromTrackedCooldowns("Cooldown snapshot received");
            }

            if (TimeCookieMessageParser.TryParseReduction(snapshot.Text, out var reduction) &&
                _cooldownTracker.ApplyTimeCookieReduction(reduction))
            {
                _log.Info($"Time cookie detected: reduced cooldown visuals by {(int)reduction.TotalMinutes} minute(s).");
                SyncEngineFromTrackedCooldowns("Tracked cooldowns reduced from time cookie");
            }

            TryUpdateConfiguredAreaFromProfile(snapshot.Text);
        }

        private bool TryRememberProcessedMessage(string messageId)
        {
            if (!_processedMessageIds.Add(messageId))
            {
                return false;
            }

            _processedMessageOrder.Enqueue(messageId);
            while (_processedMessageOrder.Count > ProcessedMessageLimit)
            {
                _processedMessageIds.Remove(_processedMessageOrder.Dequeue());
            }

            return true;
        }

        private bool TryStopEngineForPreviousCommandBusy(DiscordMessageSnapshot snapshot)
        {
            if (_engine == null ||
                !_engine.IsRunning ||
                !TrackedCommandResponseClassifier.IsPreviousCommandBusyReply(snapshot))
            {
                return false;
            }

            _engine.Stop();
            _log.Engine("Engine stopped: EPIC RPG said to end the previous command first.");
            return true;
        }

        private void SyncEngineFromTrackedCooldowns(string source)
        {
            if (_engine == null || !_engine.IsRunning)
            {
                return;
            }

            var snapshot = _cooldownTracker.GetTrackedSnapshot();
            if (_engine.TryInitializeFromCooldownSnapshot(snapshot.Daily, snapshot.Weekly, snapshot.Hunt, snapshot.Adventure, snapshot.Training, snapshot.Work, snapshot.Farm, snapshot.Lootbox))
            {
                _log.Engine(source + "; command scheduling initialized");
                return;
            }

            _engine.SyncTrackedCooldowns(snapshot.Daily, snapshot.Weekly, snapshot.Hunt, snapshot.Adventure, snapshot.Training, snapshot.Work, snapshot.Farm, snapshot.Lootbox);
            _log.Engine(source + "; scheduling resynced from tracked cooldowns");
        }

        private void TryUpdateConfiguredAreaFromProfile(string message)
        {
            if (!ProfileMessageParser.TryParseMaxArea(message, out var maxArea))
            {
                return;
            }

            var currentSettings = GetCurrentSettings();
            var currentArea = currentSettings.GetAreaOrDefault(10);
            if (currentArea == maxArea)
            {
                return;
            }

            _settingsService.Save(currentSettings.WithArea(maxArea.ToString()));
            _log.Info($"Profile detected: updated configured area to {maxArea} from profile max area.");
        }
    }
}
