namespace EpicRPGBot.UI.Models
{
    public sealed class CooldownStatsSnapshot
    {
        public CooldownStatsSnapshot(
            int totalRunning,
            int totalCount,
            int rewardsRunning,
            int rewardsCount,
            int experienceRunning,
            int experienceCount,
            int progressRunning,
            int progressCount)
        {
            TotalRunning = totalRunning;
            TotalCount = totalCount;
            RewardsRunning = rewardsRunning;
            RewardsCount = rewardsCount;
            ExperienceRunning = experienceRunning;
            ExperienceCount = experienceCount;
            ProgressRunning = progressRunning;
            ProgressCount = progressCount;
        }

        public int TotalRunning { get; }
        public int TotalCount { get; }
        public int RewardsRunning { get; }
        public int RewardsCount { get; }
        public int ExperienceRunning { get; }
        public int ExperienceCount { get; }
        public int ProgressRunning { get; }
        public int ProgressCount { get; }

        public bool HasSameCounts(CooldownStatsSnapshot other)
        {
            return other != null
                && TotalRunning == other.TotalRunning
                && TotalCount == other.TotalCount
                && RewardsRunning == other.RewardsRunning
                && RewardsCount == other.RewardsCount
                && ExperienceRunning == other.ExperienceRunning
                && ExperienceCount == other.ExperienceCount
                && ProgressRunning == other.ProgressRunning
                && ProgressCount == other.ProgressCount;
        }
    }
}
