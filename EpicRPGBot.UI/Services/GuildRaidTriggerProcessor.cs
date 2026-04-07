using System;
using System.Collections.Generic;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class GuildRaidTriggerProcessor
    {
        private const int RememberedMessageLimit = 64;

        private readonly HashSet<string> _processedMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedMessageOrder = new Queue<string>();

        public bool ShouldTrigger(AppSettingsSnapshot settings, DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null ||
                string.IsNullOrWhiteSpace(snapshot.Id) ||
                !TryRememberProcessedMessage(snapshot.Id))
            {
                return false;
            }

            return GuildRaidTriggerMatcher.IsMatch(settings, snapshot);
        }

        public void Reset()
        {
            _processedMessageIds.Clear();
            _processedMessageOrder.Clear();
        }

        private bool TryRememberProcessedMessage(string messageId)
        {
            if (!_processedMessageIds.Add(messageId))
            {
                return false;
            }

            _processedMessageOrder.Enqueue(messageId);
            while (_processedMessageOrder.Count > RememberedMessageLimit)
            {
                _processedMessageIds.Remove(_processedMessageOrder.Dequeue());
            }

            return true;
        }
    }
}
