using System;

namespace EpicRPGBot.UI.Models
{
    public sealed class AppSettingsSnapshot
    {
        public AppSettingsSnapshot(
            string channelUrl,
            bool useAtMeFallback,
            string area,
            string huntMs,
            string adventureMs,
            string workMs,
            string farmMs,
            string lootboxMs)
        {
            ChannelUrl = channelUrl ?? string.Empty;
            UseAtMeFallback = useAtMeFallback;
            Area = area ?? string.Empty;
            HuntMs = huntMs ?? string.Empty;
            AdventureMs = adventureMs ?? string.Empty;
            WorkMs = workMs ?? string.Empty;
            FarmMs = farmMs ?? string.Empty;
            LootboxMs = lootboxMs ?? string.Empty;
        }

        public string ChannelUrl { get; }

        public bool UseAtMeFallback { get; }

        public string Area { get; }

        public string HuntMs { get; }

        public string AdventureMs { get; }

        public string WorkMs { get; }

        public string FarmMs { get; }

        public string LootboxMs { get; }

        public static AppSettingsSnapshot Default =>
            new AppSettingsSnapshot(
                "https://discord.com/channels/@me",
                true,
                "10",
                "61000",
                "61000",
                "99000",
                "196000",
                "21600000");

        public string ResolveChannelUrl()
        {
            var url = ChannelUrl.Trim();
            if (string.IsNullOrEmpty(url) && UseAtMeFallback)
            {
                return "https://discord.com/channels/@me";
            }

            return string.IsNullOrEmpty(url) ? "https://discord.com/channels/@me" : url;
        }

        public int GetAreaOrDefault(int defaultValue)
        {
            return ParseOrDefault(Area, defaultValue);
        }

        public int GetHuntMsOrDefault(int defaultValue)
        {
            return ParseOrDefault(HuntMs, defaultValue);
        }

        public int GetAdventureMsOrDefault(int defaultValue)
        {
            return ParseOrDefault(AdventureMs, defaultValue);
        }

        public int GetWorkMsOrDefault(int defaultValue)
        {
            return ParseOrDefault(WorkMs, defaultValue);
        }

        public int GetFarmMsOrDefault(int defaultValue)
        {
            return ParseOrDefault(FarmMs, defaultValue);
        }

        public int GetLootboxMsOrDefault(int defaultValue)
        {
            return ParseOrDefault(LootboxMs, defaultValue);
        }

        public AppSettingsSnapshot WithChannelUrl(string channelUrl)
        {
            return new AppSettingsSnapshot(channelUrl, UseAtMeFallback, Area, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithUseAtMeFallback(bool useAtMeFallback)
        {
            return new AppSettingsSnapshot(ChannelUrl, useAtMeFallback, Area, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithArea(string area)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, area, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithHuntMs(string huntMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, huntMs, AdventureMs, WorkMs, FarmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithAdventureMs(string adventureMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, HuntMs, adventureMs, WorkMs, FarmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithWorkMs(string workMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, HuntMs, AdventureMs, workMs, FarmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithFarmMs(string farmMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, HuntMs, AdventureMs, WorkMs, farmMs, LootboxMs);
        }

        public AppSettingsSnapshot WithLootboxMs(string lootboxMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, HuntMs, AdventureMs, WorkMs, FarmMs, lootboxMs);
        }

        private static int ParseOrDefault(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }
    }
}
