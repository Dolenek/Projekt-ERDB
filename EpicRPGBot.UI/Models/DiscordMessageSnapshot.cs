namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageSnapshot
    {
        public DiscordMessageSnapshot(string id, string text, string author = null)
        {
            Id = id ?? string.Empty;
            Text = text ?? string.Empty;
            Author = author ?? string.Empty;
        }

        public string Id { get; }
        public string Text { get; }
        public string Author { get; }
    }
}
