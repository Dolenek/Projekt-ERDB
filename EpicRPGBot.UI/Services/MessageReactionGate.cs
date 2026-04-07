using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.Services
{
    public sealed class MessageReactionGate
    {
        private const int RememberedMessageLimit = 128;

        private readonly HashSet<string> _processedMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedMessageOrder = new Queue<string>();

        public bool TryBegin(string messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return true;
            }

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

        public void Reset()
        {
            _processedMessageIds.Clear();
            _processedMessageOrder.Clear();
        }
    }
}
