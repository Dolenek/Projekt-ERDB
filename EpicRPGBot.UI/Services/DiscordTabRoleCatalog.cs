using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordTabRoleCatalog
    {
        public static DiscordTabRole Parse(string value)
        {
            return Enum.TryParse(value, true, out DiscordTabRole role)
                ? role
                : DiscordTabRole.Unknown;
        }

        public static string ToMarker(DiscordTabRole role)
        {
            return role == DiscordTabRole.Unknown
                ? string.Empty
                : role.ToString().ToLowerInvariant();
        }
    }
}
