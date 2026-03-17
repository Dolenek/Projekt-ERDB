using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class CooldownTracker
    {
        private static readonly Brush ReadyBrush = CreateBrush(0x24, 0x63, 0x3C);
        private static readonly Brush AlternateReadyBrush = CreateBrush(0x2F, 0x7A, 0x4A);
        private static readonly string[] TrackedKeys = { "hunt", "adventure", "work", "farm", "lootbox" };
        private static readonly CooldownDefinition[] Definitions =
        {
            new CooldownDefinition("daily", "DailyCdRow", "DailyCdText", CooldownCategory.Rewards, "daily"),
            new CooldownDefinition("weekly", "WeeklyCdRow", "WeeklyCdText", CooldownCategory.Rewards, "weekly"),
            new CooldownDefinition("lootbox", "LootboxCdRow", "LootboxCdText", CooldownCategory.Rewards, "lootbox"),
            new CooldownDefinition("card_hand", "CardHandCdRow", "CardHandCdText", CooldownCategory.Rewards, "card hand"),
            new CooldownDefinition("vote", "VoteCdRow", "VoteCdText", CooldownCategory.Rewards, "vote"),
            new CooldownDefinition("hunt", "HuntCdRow", "HuntCdText", CooldownCategory.Experience, "hunt"),
            new CooldownDefinition("adventure", "AdventureCdRow", "AdventureCdText", CooldownCategory.Experience, "adventure"),
            new CooldownDefinition("training", "TrainingCdRow", "TrainingCdText", CooldownCategory.Experience, "training"),
            new CooldownDefinition("duel", "DuelCdRow", "DuelCdText", CooldownCategory.Experience, "duel"),
            new CooldownDefinition("quest", "QuestCdRow", "QuestCdText", CooldownCategory.Experience, "quest", "epic quest"),
            new CooldownDefinition("work", "WorkCdRow", "WorkCdText", CooldownCategory.Progress, "chop", "fish", "pickup", "mine"),
            new CooldownDefinition("farm", "FarmCdRow", "FarmCdText", CooldownCategory.Progress, "farm"),
            new CooldownDefinition("horse", "HorseCdRow", "HorseCdText", CooldownCategory.Progress, "horse breeding", "horse race"),
            new CooldownDefinition("arena", "ArenaCdRow", "ArenaCdText", CooldownCategory.Progress, "arena"),
            new CooldownDefinition("dungeon", "DungeonCdRow", "DungeonCdText", CooldownCategory.Progress, "dungeon", "miniboss")
        };

        private readonly Dictionary<string, CooldownEntry> _entries = new Dictionary<string, CooldownEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _aliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly DispatcherTimer _timer;
        private CooldownStatsSnapshot _lastStats;

        public event Action<CooldownStatsSnapshot> StatsChanged;

        public CooldownTracker(FrameworkElement root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            for (var index = 0; index < Definitions.Length; index++)
            {
                var definition = Definitions[index];
                var label = root.FindName(definition.LabelName) as TextBlock;
                var row = root.FindName(definition.RowName) as Border;
                var readyBackground = index % 2 == 0 ? ReadyBrush : AlternateReadyBrush;
                _entries[definition.CanonicalKey] = new CooldownEntry(label, row, readyBackground);

                foreach (var alias in definition.Aliases)
                {
                    _aliasMap[NormalizeAlias(alias)] = definition.CanonicalKey;
                }
            }

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (sender, args) => Tick();
            _lastStats = BuildStatsSnapshot();
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
        }

        public bool ApplyMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                message.IndexOf("cooldowns", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var changed = false;
            var lines = message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in lines)
            {
                var line = NormalizeCooldownLine(raw);
                if (ShouldSkipLine(line))
                {
                    continue;
                }

                var namesPart = ExtractNamesPart(line, out var durationRaw);
                if (string.IsNullOrWhiteSpace(namesPart))
                {
                    continue;
                }

                var duration = ParseDuration(durationRaw);

                foreach (var alias in namesPart.Split('|'))
                {
                    var normalized = NormalizeAlias(alias);
                    if (_aliasMap.TryGetValue(normalized, out var canonical) && _entries.TryGetValue(canonical, out var entry))
                    {
                        entry.Remaining = duration.HasValue && duration.Value > TimeSpan.Zero ? duration : null;
                        UpdateEntryVisual(entry);
                        changed = true;
                    }
                }
            }

            if (changed)
            {
                PublishStatsIfChanged();
            }

            return changed;
        }

        public TimeSpan? GetRemaining(string canonical)
        {
            return _entries.TryGetValue(canonical, out var entry) ? entry.Remaining : null;
        }

        public TrackedCooldownSnapshot GetTrackedSnapshot()
        {
            return new TrackedCooldownSnapshot(
                GetRemaining("hunt"),
                GetRemaining("adventure"),
                GetRemaining("work"),
                GetRemaining("farm"),
                GetRemaining("lootbox"));
        }

        public CooldownStatsSnapshot GetStatsSnapshot()
        {
            return BuildStatsSnapshot();
        }

        public void SetCooldown(string canonical, int milliseconds)
        {
            if (!_entries.TryGetValue(canonical, out var entry))
            {
                return;
            }

            entry.Remaining = milliseconds > 0
                ? TimeSpan.FromMilliseconds(milliseconds)
                : (TimeSpan?)null;
            UpdateEntryVisual(entry);
            PublishStatsIfChanged();
        }

        public bool ApplyTimeCookieReduction(TimeSpan reduction)
        {
            if (reduction <= TimeSpan.Zero)
            {
                return false;
            }

            var changed = false;
            foreach (var canonical in TrackedKeys)
            {
                if (!_entries.TryGetValue(canonical, out var entry) || !entry.Remaining.HasValue)
                {
                    continue;
                }

                var next = entry.Remaining.Value - reduction;
                entry.Remaining = next > TimeSpan.Zero ? next : (TimeSpan?)null;
                UpdateEntryVisual(entry);
                changed = true;
            }

            if (changed)
            {
                PublishStatsIfChanged();
            }

            return changed;
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
                UpdateEntryVisual(entry);
            }

            PublishStatsIfChanged();
        }

        private static bool ShouldSkipLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return true;
            }

            var header = line.ToLowerInvariant();
            return header.Contains("cooldowns")
                || header.Contains("cooldown reduction")
                || header == "rewards"
                || header == "experience"
                || header == "progress";
        }

        private static string NormalizeCooldownLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var cleaned = Regex.Replace(raw.Trim(), @"^[^\p{L}\p{N}]+", string.Empty);
            return cleaned
                .Replace("`", string.Empty)
                .Replace("~", string.Empty)
                .Replace("*", string.Empty)
                .Trim();
        }

        private static string NormalizeAlias(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var cleaned = Regex.Replace(raw, @"[\u200B-\u200D\uFEFF]", string.Empty)
                .Replace("`", string.Empty)
                .Replace("~", string.Empty)
                .Replace("*", string.Empty)
                .Trim();

            cleaned = Regex.Replace(cleaned, @"^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$", string.Empty);
            return Regex.Replace(cleaned, @"\s+", " ").Trim().ToLowerInvariant();
        }

        private static string ExtractNamesPart(string line, out string durationRaw)
        {
            durationRaw = null;
            if (string.IsNullOrWhiteSpace(line))
            {
                return string.Empty;
            }

            line = Regex.Replace(line, @"[\u200B-\u200D\uFEFF]", string.Empty).Trim();
            var openIndex = line.LastIndexOf('(');
            var closeIndex = line.LastIndexOf(')');
            if (openIndex < 0 || closeIndex <= openIndex)
            {
                return line.Trim();
            }

            durationRaw = line.Substring(openIndex + 1, closeIndex - openIndex - 1).Trim();
            return line.Substring(0, openIndex).Trim();
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

        private static Brush CreateBrush(byte red, byte green, byte blue)
        {
            var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
            brush.Freeze();
            return brush;
        }

        private static void UpdateEntryVisual(CooldownEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            if (entry.Label != null)
            {
                entry.Label.Text = entry.Remaining.HasValue ? FormatDuration(entry.Remaining.Value) : "Ready";
            }

            if (entry.Row != null)
            {
                entry.Row.Background = entry.Remaining.HasValue ? Brushes.Transparent : entry.ReadyBackground;
            }
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

        private CooldownStatsSnapshot BuildStatsSnapshot()
        {
            return CooldownStatsCalculator.BuildSnapshot(
                Definitions,
                canonical => _entries.TryGetValue(canonical, out var entry) && entry.Remaining.HasValue);
        }

        private void PublishStatsIfChanged()
        {
            var snapshot = BuildStatsSnapshot();
            if (_lastStats != null && _lastStats.HasSameCounts(snapshot))
            {
                return;
            }

            _lastStats = snapshot;
            StatsChanged?.Invoke(snapshot);
        }

        private sealed class CooldownEntry
        {
            public CooldownEntry(TextBlock label, Border row, Brush readyBackground)
            {
                Label = label;
                Row = row;
                ReadyBackground = readyBackground;
            }

            public TextBlock Label { get; }
            public Border Row { get; }
            public Brush ReadyBackground { get; }
            public TimeSpan? Remaining { get; set; }
        }
    }
}
