using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonBattleState
    {
        public DungeonBattleState(
            bool hasEncounter,
            bool shouldBite,
            bool hasVictory,
            bool hasFailure,
            string activitySignature,
            DiscordMessageSnapshot deletePrompt)
        {
            HasEncounter = hasEncounter;
            ShouldBite = shouldBite;
            HasVictory = hasVictory;
            HasFailure = hasFailure;
            ActivitySignature = activitySignature ?? string.Empty;
            DeletePrompt = deletePrompt;
        }

        public bool HasEncounter { get; }

        public bool ShouldBite { get; }

        public bool HasVictory { get; }

        public bool HasFailure { get; }

        public string ActivitySignature { get; }

        public DiscordMessageSnapshot DeletePrompt { get; }
    }
}
