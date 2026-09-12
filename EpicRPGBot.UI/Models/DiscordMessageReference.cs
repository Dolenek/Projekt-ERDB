using System;

namespace EpicRPGBot.UI.Models
{
    public sealed class DiscordMessageReference
    {
        public DiscordMessageReference(string messageElementId, DiscordTabRole tabRole, string channelUrl)
        {
            MessageElementId = messageElementId ?? string.Empty;
            TabRole = tabRole;
            ChannelUrl = channelUrl ?? string.Empty;
        }

        public string MessageElementId { get; }

        public DiscordTabRole TabRole { get; }

        public string ChannelUrl { get; }

        public bool IsComplete =>
            !string.IsNullOrWhiteSpace(MessageElementId) &&
            TabRole != DiscordTabRole.Unknown &&
            !string.IsNullOrWhiteSpace(ChannelUrl);

        public static DiscordMessageReference FromSnapshot(DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            var reference = new DiscordMessageReference(
                snapshot.Id,
                snapshot.SourceTabRole,
                snapshot.ChannelUrl);
            return reference.IsComplete ? reference : null;
        }
    }
}
