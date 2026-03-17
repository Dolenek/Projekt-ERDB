using System.Windows.Controls;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private TextBlock _huntCountText;
        private TextBlock _adventureCountText;
        private TextBlock _workCountText;
        private TextBlock _farmCountText;
        private TextBlock _lootboxCountText;
        private TextBlock _runningCooldownsText;
        private TextBlock _runningRewardsText;
        private TextBlock _runningExperienceText;
        private TextBlock _runningProgressText;
        private int _huntCount;
        private int _adventureCount;
        private int _workCount;
        private int _farmCount;
        private int _lootboxCount;

        private void BindStatsUi()
        {
            _huntCountText = FindName("HuntCountText") as TextBlock;
            _adventureCountText = FindName("AdventureCountText") as TextBlock;
            _workCountText = FindName("WorkCountText") as TextBlock;
            _farmCountText = FindName("FarmCountText") as TextBlock;
            _lootboxCountText = FindName("LootboxCountText") as TextBlock;
            _runningCooldownsText = FindName("RunningCooldownsText") as TextBlock;
            _runningRewardsText = FindName("RunningRewardsText") as TextBlock;
            _runningExperienceText = FindName("RunningExperienceText") as TextBlock;
            _runningProgressText = FindName("RunningProgressText") as TextBlock;

            _cooldownTracker.StatsChanged += OnCooldownStatsChanged;
            UpdateSentCountTexts();
            ApplyCooldownStats(_cooldownTracker.GetStatsSnapshot());
        }

        private void ReleaseStatsUi()
        {
            _cooldownTracker.StatsChanged -= OnCooldownStatsChanged;
        }

        private void TrackSentCommandStats(string command)
        {
            switch (GetTrackedCommandKey(command))
            {
                case "hunt":
                    _huntCount++;
                    break;
                case "adventure":
                    _adventureCount++;
                    break;
                case "work":
                    _workCount++;
                    break;
                case "farm":
                    _farmCount++;
                    break;
                case "lootbox":
                    _lootboxCount++;
                    break;
                default:
                    return;
            }

            UpdateSentCountTexts();
        }

        private void OnCooldownStatsChanged(CooldownStatsSnapshot snapshot)
        {
            UiDispatcher.OnUI(() => ApplyCooldownStats(snapshot));
        }

        private void ApplyCooldownStats(CooldownStatsSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            SetStatText(_runningCooldownsText, $"Running cooldowns: {snapshot.TotalRunning} / {snapshot.TotalCount}");
            SetStatText(_runningRewardsText, $"Rewards running: {snapshot.RewardsRunning} / {snapshot.RewardsCount}");
            SetStatText(_runningExperienceText, $"Experience running: {snapshot.ExperienceRunning} / {snapshot.ExperienceCount}");
            SetStatText(_runningProgressText, $"Progress running: {snapshot.ProgressRunning} / {snapshot.ProgressCount}");
        }

        private void UpdateSentCountTexts()
        {
            SetStatText(_huntCountText, $"Hunt sent: {_huntCount}");
            SetStatText(_adventureCountText, $"Adventure sent: {_adventureCount}");
            SetStatText(_workCountText, $"Work sent: {_workCount}");
            SetStatText(_farmCountText, $"Farm sent: {_farmCount}");
            SetStatText(_lootboxCountText, $"Lootbox sent: {_lootboxCount}");
        }

        private static string GetTrackedCommandKey(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            var normalized = command.Trim().ToLowerInvariant();
            if (normalized.StartsWith("rpg hunt", System.StringComparison.Ordinal))
            {
                return "hunt";
            }

            if (normalized.StartsWith("rpg adv", System.StringComparison.Ordinal))
            {
                return "adventure";
            }

            if (normalized.StartsWith("rpg farm", System.StringComparison.Ordinal))
            {
                return "farm";
            }

            if (normalized.StartsWith("rpg buy ed lb", System.StringComparison.Ordinal))
            {
                return "lootbox";
            }

            if (normalized.StartsWith("rpg chop", System.StringComparison.Ordinal) ||
                normalized.StartsWith("rpg axe", System.StringComparison.Ordinal) ||
                normalized.StartsWith("rpg bowsaw", System.StringComparison.Ordinal) ||
                normalized.StartsWith("rpg chainsaw", System.StringComparison.Ordinal))
            {
                return "work";
            }

            return null;
        }

        private static void SetStatText(TextBlock target, string value)
        {
            if (target != null)
            {
                target.Text = value;
            }
        }
    }
}
