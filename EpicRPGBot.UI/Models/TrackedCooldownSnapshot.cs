using System;

namespace EpicRPGBot.UI.Models
{
    public sealed class TrackedCooldownSnapshot
    {
        public TrackedCooldownSnapshot(
            TimeSpan? daily,
            TimeSpan? weekly,
            TimeSpan? hunt,
            TimeSpan? adventure,
            TimeSpan? training,
            TimeSpan? work,
            TimeSpan? farm,
            TimeSpan? lootbox)
        {
            Daily = daily;
            Weekly = weekly;
            Hunt = hunt;
            Adventure = adventure;
            Training = training;
            Work = work;
            Farm = farm;
            Lootbox = lootbox;
        }

        public TimeSpan? Daily { get; }
        public TimeSpan? Weekly { get; }
        public TimeSpan? Hunt { get; }
        public TimeSpan? Adventure { get; }
        public TimeSpan? Training { get; }
        public TimeSpan? Work { get; }
        public TimeSpan? Farm { get; }
        public TimeSpan? Lootbox { get; }
    }
}
