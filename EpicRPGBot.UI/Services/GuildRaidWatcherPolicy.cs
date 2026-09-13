using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public static class GuildRaidWatcherPolicy
    {
        public static bool ShouldRun(AppSettingsSnapshot settings)
        {
            return settings?.GuildRaidWatcherActive == true &&
                   settings.IsGuildRaidConfigured() &&
                   settings.TryResolveGuildRaidChannelUrl(out _);
        }
    }
}
