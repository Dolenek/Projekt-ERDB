namespace EpicRPGBot.UI.Bunny
{
    public sealed class BunnyPromptParseResult
    {
        public BunnyPromptParseResult(
            bool isBunnyPrompt,
            bool hasReadableStats,
            int happiness,
            int hunger,
            string summary)
        {
            IsBunnyPrompt = isBunnyPrompt;
            HasReadableStats = hasReadableStats;
            Happiness = happiness;
            Hunger = hunger;
            Summary = summary ?? string.Empty;
        }

        public bool IsBunnyPrompt { get; }
        public bool HasReadableStats { get; }
        public int Happiness { get; }
        public int Hunger { get; }
        public string Summary { get; }
    }
}
