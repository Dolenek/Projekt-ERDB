namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageReaction
    {
        public DiscordMessageReaction(string name, bool isMine, int count)
        {
            Name = name ?? string.Empty;
            IsMine = isMine;
            Count = count;
        }

        public string Name { get; }

        public bool IsMine { get; }

        public int Count { get; }
    }
}
