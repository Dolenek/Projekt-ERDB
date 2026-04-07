namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageMention
    {
        public DiscordMessageMention(string label, string userId)
        {
            Label = label ?? string.Empty;
            UserId = userId ?? string.Empty;
        }

        public string Label { get; }

        public string UserId { get; }
    }
}
