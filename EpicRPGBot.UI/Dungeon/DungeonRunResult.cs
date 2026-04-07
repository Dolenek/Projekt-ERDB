namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonRunResult
    {
        private DungeonRunResult(bool completed, bool cancelled, string summary)
        {
            Completed = completed;
            Cancelled = cancelled;
            Summary = summary ?? string.Empty;
        }

        public bool Completed { get; }

        public bool Cancelled { get; }

        public string Summary { get; }

        public static DungeonRunResult CompletedResult(string summary)
        {
            return new DungeonRunResult(true, false, summary);
        }

        public static DungeonRunResult FailedResult(string summary)
        {
            return new DungeonRunResult(false, false, summary);
        }

        public static DungeonRunResult CancelledResult(string summary)
        {
            return new DungeonRunResult(false, true, summary);
        }
    }
}
