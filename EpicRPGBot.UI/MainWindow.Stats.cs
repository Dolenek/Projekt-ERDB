using System.Windows.Controls;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private TextBlock _huntCountText;
        private TextBlock _adventureCountText;
        private TextBlock _trainingCountText;
        private TextBlock _workCountText;
        private TextBlock _farmCountText;
        private TextBlock _lootboxCountText;
        private TextBlock _runningCooldownsText;
        private TextBlock _runningRewardsText;
        private TextBlock _runningExperienceText;
        private TextBlock _runningProgressText;
        private int _huntCount { get => CurrentAccount.HuntCount; set => CurrentAccount.HuntCount = value; }
        private int _adventureCount { get => CurrentAccount.AdventureCount; set => CurrentAccount.AdventureCount = value; }
        private int _trainingCount { get => CurrentAccount.TrainingCount; set => CurrentAccount.TrainingCount = value; }
        private int _workCount { get => CurrentAccount.WorkCount; set => CurrentAccount.WorkCount = value; }
        private int _farmCount { get => CurrentAccount.FarmCount; set => CurrentAccount.FarmCount = value; }
        private int _lootboxCount { get => CurrentAccount.LootboxCount; set => CurrentAccount.LootboxCount = value; }

        private void BindStatsUi()
        {
            _huntCountText = FindName("HuntCountText") as TextBlock;
            _adventureCountText = FindName("AdventureCountText") as TextBlock;
            _trainingCountText = FindName("TrainingCountText") as TextBlock;
            _workCountText = FindName("WorkCountText") as TextBlock;
            _farmCountText = FindName("FarmCountText") as TextBlock;
            _lootboxCountText = FindName("LootboxCountText") as TextBlock;
            _runningCooldownsText = FindName("RunningCooldownsText") as TextBlock;
            _runningRewardsText = FindName("RunningRewardsText") as TextBlock;
            _runningExperienceText = FindName("RunningExperienceText") as TextBlock;
            _runningProgressText = FindName("RunningProgressText") as TextBlock;

            UpdateSentCountTexts();
            ApplyCooldownStats(_cooldownTracker.GetStatsSnapshot());
        }

        private void ReleaseStatsUi()
        {
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
                case "training":
                    _trainingCount++;
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
            if (snapshot == null || !ReferenceEquals(CurrentAccount, _activeAccountRuntime))
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
            if (!ReferenceEquals(CurrentAccount, _activeAccountRuntime)) return;
            SetStatText(_huntCountText, $"Hunt sent: {_huntCount}");
            SetStatText(_adventureCountText, $"Adventure sent: {_adventureCount}");
            SetStatText(_trainingCountText, $"Training sent: {_trainingCount}");
            SetStatText(_workCountText, $"Work sent: {_workCount}");
            SetStatText(_farmCountText, $"Farm sent: {_farmCount}");
            SetStatText(_lootboxCountText, $"Lootbox sent: {_lootboxCount}");
        }

        private string GetTrackedCommandKey(string command)
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

            if (normalized.StartsWith("rpg daily", System.StringComparison.Ordinal))
            {
                return "daily";
            }

            if (normalized.StartsWith("rpg weekly", System.StringComparison.Ordinal))
            {
                return "weekly";
            }

            if (normalized.StartsWith("rpg card hand", System.StringComparison.Ordinal))
            {
                return "card_hand";
            }

            if (normalized.StartsWith("rpg adv", System.StringComparison.Ordinal))
            {
                return "adventure";
            }

            if (normalized.StartsWith("rpg tr", System.StringComparison.Ordinal))
            {
                return "training";
            }

            if (normalized.StartsWith("rpg farm", System.StringComparison.Ordinal))
            {
                return "farm";
            }

            if (normalized.StartsWith("rpg buy ed lb", System.StringComparison.Ordinal))
            {
                return "lootbox";
            }

            if (ConfiguredWorkCommandCatalog.IsWorkCommand(normalized, GetCurrentSettings().WorkCommands))
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
