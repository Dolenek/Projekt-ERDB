using System.Collections.Generic;

namespace EpicRPGBot.UI.Services
{
    public sealed class DiscordWebViewDemandTracker
    {
        private readonly Dictionary<DiscordWebViewActivityReason, int> _demandCounts =
            new Dictionary<DiscordWebViewActivityReason, int>();

        public bool HasDemand => _demandCounts.Count > 0;

        public void Set(DiscordWebViewActivityReason reason, bool isRequired)
        {
            if (isRequired)
            {
                _demandCounts[reason] = System.Math.Max(1, GetCount(reason));
            }
            else
            {
                _demandCounts.Remove(reason);
            }
        }

        public void Add(DiscordWebViewActivityReason reason)
        {
            _demandCounts[reason] = GetCount(reason) + 1;
        }

        public void Remove(DiscordWebViewActivityReason reason)
        {
            var nextCount = GetCount(reason) - 1;
            if (nextCount > 0) _demandCounts[reason] = nextCount;
            else _demandCounts.Remove(reason);
        }

        public int GetCount(DiscordWebViewActivityReason reason)
        {
            return _demandCounts.TryGetValue(reason, out var count) ? count : 0;
        }

        public void Clear()
        {
            _demandCounts.Clear();
        }
    }
}
