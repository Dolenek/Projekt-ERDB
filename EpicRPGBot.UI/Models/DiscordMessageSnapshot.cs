namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageSnapshot
    {
        public DiscordMessageSnapshot(string id, string text)
        {
            Id = id ?? string.Empty;
            Text = text ?? string.Empty;
        }

        public string Id { get; }
        public string Text { get; }
    }
}
