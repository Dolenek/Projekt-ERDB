namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelRunResult
    {
        private DuelRunResult(bool completed, bool requiresBotToRemainStopped, string summary)
        {
            Completed = completed;
            RequiresBotToRemainStopped = requiresBotToRemainStopped;
            Summary = summary ?? string.Empty;
        }

        public bool Completed { get; }

        public bool RequiresBotToRemainStopped { get; }

        public string Summary { get; }

        public static DuelRunResult CompletedResult(string summary) => new DuelRunResult(true, false, summary);

        public static DuelRunResult FailedResult(string summary, bool requiresStopped = false) =>
            new DuelRunResult(false, requiresStopped, summary);

        public static DuelRunResult CancelledResult(bool requiresStopped)
        {
            var summary = requiresStopped
                ? "Duel automation stopped; bot remains stopped because a duel may still be active."
                : "Duel automation stopped.";
            return new DuelRunResult(false, requiresStopped, summary);
        }
    }
}
