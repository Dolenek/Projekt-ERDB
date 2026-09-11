using EpicRPGBot.UI.Models;
using System;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class CooldownTracker
    {
        public TimeSpan? GetRemaining(string canonical)
        {
            return _entries.TryGetValue(canonical, out var entry) ? entry.Remaining : null;
        }

        public CooldownStatsSnapshot GetStatsSnapshot()
        {
            return BuildStatsSnapshot();
        }

        public void SetCooldown(string canonical, int milliseconds)
        {
            if (!_entries.TryGetValue(canonical, out var entry)) return;
            entry.Remaining = milliseconds > 0
                ? TimeSpan.FromMilliseconds(milliseconds)
                : (TimeSpan?)null;
            UpdateEntryVisual(entry);
            PublishStatsIfChanged();
        }

        public void RefreshWorkAliases(string serializedSelections)
        {
            foreach (var alias in _workAliases) _aliasMap.Remove(alias);
            _workAliases.Clear();
            foreach (var alias in ConfiguredWorkCommandCatalog.BuildCooldownAliases(serializedSelections))
            {
                var normalized = NormalizeAlias(alias);
                if (string.IsNullOrWhiteSpace(normalized)) continue;
                _aliasMap[normalized] = "work";
                _workAliases.Add(normalized);
            }
        }

        public TrackedCooldownSnapshot GetTrackedSnapshot()
        {
            return new TrackedCooldownSnapshot(
                GetRemaining("daily"),
                GetRemaining("weekly"),
                GetRemaining("hunt"),
                GetRemaining("adventure"),
                GetRemaining("training"),
                GetRemaining("work"),
                GetRemaining("farm"),
                GetRemaining("lootbox"),
                GetRemaining("card_hand"));
        }
    }
}
