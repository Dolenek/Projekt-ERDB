namespace EpicRPGBot.UI.Crafting
{
    public sealed class CraftJobResult
    {
        private CraftJobResult(bool completed, bool cancelled, string summary)
        {
            Completed = completed;
            Cancelled = cancelled;
            Summary = summary ?? string.Empty;
        }

        public bool Completed { get; }
        public bool Cancelled { get; }
        public string Summary { get; }

        public static CraftJobResult CompletedResult(string summary)
        {
            return new CraftJobResult(true, false, summary);
        }

        public static CraftJobResult CancelledResult(string summary)
        {
            return new CraftJobResult(false, true, summary);
        }

        public static CraftJobResult FailedResult(string summary)
        {
            return new CraftJobResult(false, false, summary);
        }
    }
}
