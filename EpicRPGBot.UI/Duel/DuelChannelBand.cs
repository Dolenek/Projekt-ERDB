namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelChannelBand
    {
        public DuelChannelBand(string channelName, int minimumLevel, int? maximumLevel)
        {
            ChannelName = channelName;
            MinimumLevel = minimumLevel;
            MaximumLevel = maximumLevel;
        }

        public string ChannelName { get; }

        public int MinimumLevel { get; }

        public int? MaximumLevel { get; }

        public bool Contains(int level)
        {
            return level >= MinimumLevel && (!MaximumLevel.HasValue || level <= MaximumLevel.Value);
        }

        public bool Intersects(int minimumLevel, long maximumLevel)
        {
            var bandMaximum = MaximumLevel ?? int.MaxValue;
            return bandMaximum >= minimumLevel && MinimumLevel <= maximumLevel;
        }
    }
}
