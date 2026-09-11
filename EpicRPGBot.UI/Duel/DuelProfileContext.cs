namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelProfileContext
    {
        public DuelProfileContext(string playerName, int level, string discordAuthorId, string discordAuthorName)
        {
            PlayerName = playerName ?? string.Empty;
            Level = level;
            DiscordAuthorId = discordAuthorId ?? string.Empty;
            DiscordAuthorName = discordAuthorName ?? string.Empty;
        }

        public string PlayerName { get; }

        public int Level { get; }

        public string DiscordAuthorId { get; }

        public string DiscordAuthorName { get; }
    }
}
