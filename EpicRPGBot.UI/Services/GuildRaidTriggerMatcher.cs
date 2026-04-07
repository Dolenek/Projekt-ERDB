using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public static class GuildRaidTriggerMatcher
    {
        public static bool IsMatch(AppSettingsSnapshot settings, DiscordMessageSnapshot snapshot)
        {
            if (settings == null ||
                snapshot == null ||
                !settings.IsGuildRaidConfigured() ||
                !settings.TryResolveGuildRaidChannelUrl(out _))
            {
                return false;
            }

            var messageText = Normalize(snapshot.RenderedText);
            var triggerText = Normalize(settings.ResolveGuildRaidTriggerText());
            if (string.IsNullOrWhiteSpace(messageText) || string.IsNullOrWhiteSpace(triggerText))
            {
                return false;
            }

            var authorFilter = Normalize(settings.ResolveGuildRaidAuthorFilter());
            if (!string.IsNullOrWhiteSpace(authorFilter))
            {
                var author = Normalize(snapshot.Author);
                if (author.IndexOf(authorFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return settings.UsesExactGuildRaidMatch()
                ? string.Equals(messageText, triggerText, StringComparison.OrdinalIgnoreCase)
                : messageText.IndexOf(triggerText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim();
        }
    }
}
