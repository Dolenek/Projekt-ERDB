using System;

namespace EpicRPGBot.UI.Models
{
    public sealed class TrackedCooldownSnapshot
    {
        public TrackedCooldownSnapshot(TimeSpan? hunt, TimeSpan? adventure, TimeSpan? work, TimeSpan? farm, TimeSpan? lootbox)
        {
            Hunt = hunt;
            Adventure = adventure;
            Work = work;
            Farm = farm;
            Lootbox = lootbox;
        }

        public TimeSpan? Hunt { get; }
        public TimeSpan? Adventure { get; }
        public TimeSpan? Work { get; }
        public TimeSpan? Farm { get; }
        public TimeSpan? Lootbox { get; }
    }
}
