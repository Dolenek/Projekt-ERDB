namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageMention
    {
        public DiscordMessageMention(string label, string userId, string commandToken = null)
        {
            Label = label ?? string.Empty;
            UserId = userId ?? string.Empty;
            CommandToken = string.IsNullOrWhiteSpace(commandToken) ? Label : commandToken;
        }

        public string Label { get; }

        public string UserId { get; }

        public string CommandToken { get; }
    }
}
