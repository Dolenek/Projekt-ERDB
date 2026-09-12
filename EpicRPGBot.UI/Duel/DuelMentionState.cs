using System;
using System.Collections.Generic;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    internal sealed class DuelMentionState
    {
        private const int StableClearPollsRequired = 3;
        private readonly ISet<string> _waitingForClear =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly IDictionary<string, int> _clearPolls =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public void Reset()
        {
            _waitingForClear.Clear();
            _clearPolls.Clear();
        }

        public IReadOnlyList<DiscordChannelReference> FindIncreases(
            IReadOnlyList<DiscordChannelReference> channels,
            IReadOnlyDictionary<string, int> current,
            IDictionary<string, int> baseline)
        {
            var signaled = new List<DiscordChannelReference>();
            foreach (var channel in channels)
            {
                var currentCount = ReadCount(current, channel.Id);
                if (_waitingForClear.Contains(channel.Id))
                {
                    ObserveClear(channel.Id, currentCount, baseline);
                    continue;
                }

                if (currentCount > ReadMutableCount(baseline, channel.Id))
                {
                    signaled.Add(channel);
                    _waitingForClear.Add(channel.Id);
                    _clearPolls[channel.Id] = 0;
                }

                baseline[channel.Id] = currentCount;
            }

            return signaled;
        }

        private void ObserveClear(
            string channelId,
            int currentCount,
            IDictionary<string, int> baseline)
        {
            if (currentCount > 0)
            {
                _clearPolls[channelId] = 0;
                baseline[channelId] = currentCount;
                return;
            }

            var clearPolls = ReadMutableCount(_clearPolls, channelId) + 1;
            _clearPolls[channelId] = clearPolls;
            baseline[channelId] = 0;
            if (clearPolls < StableClearPollsRequired)
            {
                return;
            }

            _waitingForClear.Remove(channelId);
            _clearPolls.Remove(channelId);
        }

        private static int ReadCount(IReadOnlyDictionary<string, int> counts, string channelId)
        {
            return counts != null && counts.TryGetValue(channelId, out var count) ? count : 0;
        }

        private static int ReadMutableCount(IDictionary<string, int> counts, string channelId)
        {
            return counts != null && counts.TryGetValue(channelId, out var count) ? count : 0;
        }
    }
}
