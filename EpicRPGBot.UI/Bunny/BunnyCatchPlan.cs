namespace EpicRPGBot.UI.Bunny
{
    public sealed class BunnyCatchPlan
    {
        public BunnyCatchPlan(
            bool isBunnyPrompt,
            bool hasReadableStats,
            string replyText,
            bool usedFallback,
            string summary)
        {
            IsBunnyPrompt = isBunnyPrompt;
            HasReadableStats = hasReadableStats;
            ReplyText = replyText ?? string.Empty;
            UsedFallback = usedFallback;
            Summary = summary ?? string.Empty;
        }

        public bool IsBunnyPrompt { get; }
        public bool HasReadableStats { get; }
        public string ReplyText { get; }
        public bool UsedFallback { get; }
        public string Summary { get; }
    }
}
