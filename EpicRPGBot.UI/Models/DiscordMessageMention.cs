namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageMention
    {
        public DiscordMessageMention(
            string label,
            string userId,
            string commandToken = null,
            string alternateCommandToken = null)
        {
            Label = label ?? string.Empty;
            UserId = userId ?? string.Empty;
            CommandToken = string.IsNullOrWhiteSpace(commandToken) ? Label : commandToken;
            AlternateCommandToken = alternateCommandToken ?? string.Empty;
        }

        public string Label { get; }

        public string UserId { get; }

        public string CommandToken { get; }

        public string AlternateCommandToken { get; }
    }
}
