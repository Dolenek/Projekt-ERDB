using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class CooldownTracker
    {
        private static readonly CooldownDefinition[] Definitions =
        {
            new CooldownDefinition("daily", "DailyCdText", "daily"),
            new CooldownDefinition("weekly", "WeeklyCdText", "weekly"),
            new CooldownDefinition("lootbox", "LootboxCdText", "lootbox"),
            new CooldownDefinition("card_hand", "CardHandCdText", "card hand"),
            new CooldownDefinition("vote", "VoteCdText", "vote"),
            new CooldownDefinition("hunt", "HuntCdText", "hunt"),
            new CooldownDefinition("adventure", "AdventureCdText", "adventure"),
            new CooldownDefinition("training", "TrainingCdText", "training"),
            new CooldownDefinition("duel", "DuelCdText", "duel"),
            new CooldownDefinition("quest", "QuestCdText", "quest", "epic quest"),
            new CooldownDefinition("work", "WorkCdText", "chop", "fish", "pickup", "mine"),
            new CooldownDefinition("farm", "FarmCdText", "farm"),
            new CooldownDefinition("horse", "HorseCdText", "horse breeding", "horse race"),
            new CooldownDefinition("arena", "ArenaCdText", "arena"),
            new CooldownDefinition("dungeon", "DungeonCdText", "dungeon", "miniboss")
        };

        private readonly Dictionary<string, CooldownEntry> _entries = new Dictionary<string, CooldownEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly DispatcherTimer _timer;

        public CooldownTracker(FrameworkElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            foreach (var definition in Definitions)
            {
                var label = root.FindName(definition.LabelName) as TextBlock;
                _entries[definition.CanonicalKey] = new CooldownEntry(label);

                foreach (var alias in definition.Aliases)
                {
                    _aliasMap[alias] = definition.CanonicalKey;
                }
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (sender, args) => Tick();
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public void ApplyMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                message.IndexOf("cooldowns", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (ShouldSkipLine(line))
                {
                    continue;
                }

                var match = Regex.Match(line, @"^\s*[~`\-\*]*\s*(.+?)\s*(?:\(([^)]+)\))?\s*$");
                if (!match.Success)
                {
                    continue;
                }

                var namesPart = match.Groups[1].Value.Trim();
                var duration = ParseDuration(match.Groups[2].Success ? match.Groups[2].Value.Trim() : null);

                foreach (var alias in namesPart.Split('|'))
                {
                    var normalized = alias.Trim().ToLowerInvariant();
                    if (_aliasMap.TryGetValue(normalized, out var canonical) && _entries.TryGetValue(canonical, out var entry))
                    {
                        entry.Remaining = duration.HasValue && duration.Value > TimeSpan.Zero ? duration : null;
                        UpdateLabel(entry.Label, entry.Remaining);
                    }
                }
            }
        }

        public TimeSpan? GetRemaining(string canonical)
        {
            return _entries.TryGetValue(canonical, out var entry) ? entry.Remaining : null;
        }

        private void Tick()
        {
            foreach (var entry in _entries.Values.Distinct())
            {
                if (entry.Label == null || !entry.Remaining.HasValue)
                {
                    continue;
                }

                var next = entry.Remaining.Value - TimeSpan.FromSeconds(1);
                entry.Remaining = next > TimeSpan.Zero ? next : (TimeSpan?)null;
                UpdateLabel(entry.Label, entry.Remaining);
            }
        }

        private static bool ShouldSkipLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return true;
            }

            var header = line.ToLowerInvariant();
            return header.Contains("cooldowns")
                || header == "rewards"
                || header == "experience"
                || header == "progress";
        }

        private static TimeSpan? ParseDuration(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var matches = Regex.Matches(raw, @"(\d+)\s*([dhms])", RegexOptions.IgnoreCase);
            if (matches.Count == 0)
            {
                return null;
            }

            var days = 0;
            var hours = 0;
            var minutes = 0;
            var seconds = 0;

            foreach (Match match in matches)
            {
                var value = int.Parse(match.Groups[1].Value);
                switch (match.Groups[2].Value.ToLowerInvariant())
                {
                    case "d": days = value; break;
                    case "h": hours = value; break;
                    case "m": minutes = value; break;
                    case "s": seconds = value; break;
                }
            }

            try
            {
                return new TimeSpan(days, hours, minutes, seconds);
            }
            catch
            {
                return null;
            }
        }

        private static void UpdateLabel(TextBlock label, TimeSpan? remaining)
        {
            if (label == null)
            {
                return;
            }

            label.Text = remaining.HasValue ? FormatDuration(remaining.Value) : "Ready";
        }

        private static string FormatDuration(TimeSpan duration)
        {
            var parts = new List<string>();
            if (duration.Days > 0) parts.Add($"{duration.Days}d");
            if (duration.Days > 0 || duration.Hours > 0) parts.Add($"{duration.Hours}h");
            if (duration.Days > 0 || duration.Hours > 0 || duration.Minutes > 0) parts.Add($"{duration.Minutes}m");
            parts.Add($"{duration.Seconds}s");
            return string.Join(" ", parts);
        }

        private sealed class CooldownEntry
        {
            public CooldownEntry(TextBlock label)
            {
                Label = label;
            }

            public TextBlock Label { get; }
            public TimeSpan? Remaining { get; set; }
        }
    }
}
