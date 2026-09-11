namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordChannelReference
    {
        public DiscordChannelReference(string id, string name, string url)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Url = url ?? string.Empty;
        }

        public string Id { get; }

        public string Name { get; }

        public string Url { get; }
    }
}
