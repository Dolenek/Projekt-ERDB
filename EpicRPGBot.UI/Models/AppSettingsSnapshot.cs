using System;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Models
{
    public sealed class AppSettingsSnapshot
    {
        public AppSettingsSnapshot(
            string channelUrl,
            bool useAtMeFallback,
            string area,
            bool ascended,
            string huntMs,
            string adventureMs,
            string workMs,
            string farmMs,
            string lootboxMs,
            string workCommands)
        {
            ChannelUrl = channelUrl ?? string.Empty;
            UseAtMeFallback = useAtMeFallback;
            Area = area ?? string.Empty;
            Ascended = ascended;
            HuntMs = huntMs ?? string.Empty;
            AdventureMs = adventureMs ?? string.Empty;
            WorkMs = workMs ?? string.Empty;
            FarmMs = farmMs ?? string.Empty;
            LootboxMs = lootboxMs ?? string.Empty;
            WorkCommands = string.IsNullOrWhiteSpace(workCommands)
                ? AreaWorkCommandSettings.DefaultSerializedMap
                : workCommands;
        }

        public string ChannelUrl { get; }

        public bool UseAtMeFallback { get; }

        public string Area { get; }

        public bool Ascended { get; }

        public string HuntMs { get; }

        public string AdventureMs { get; }

        public string WorkMs { get; }

        public string FarmMs { get; }

        public string LootboxMs { get; }

        public string WorkCommands { get; }

        public static AppSettingsSnapshot Default =>
            new AppSettingsSnapshot(
                "https://discord.com/channels/@me",
                true,
                "10",
                false,
                "61000",
                "61000",
                "99000",
                "196000",
                "21600000",
                AreaWorkCommandSettings.DefaultSerializedMap);

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

        public bool IsFarmAllowed(int defaultArea)
        {
            return Ascended || GetAreaOrDefault(defaultArea) >= 4;
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

        public string ResolveWorkCommandForArea(int area)
        {
            return AreaWorkCommandSettings.ResolveCommandText(WorkCommands, area);
        }

        public AppSettingsSnapshot WithChannelUrl(string channelUrl)
        {
            return new AppSettingsSnapshot(channelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithUseAtMeFallback(bool useAtMeFallback)
        {
            return new AppSettingsSnapshot(ChannelUrl, useAtMeFallback, Area, Ascended, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithArea(string area)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, area, Ascended, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithAscended(bool ascended)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, ascended, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithHuntMs(string huntMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, Ascended, huntMs, AdventureMs, WorkMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithAdventureMs(string adventureMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, adventureMs, WorkMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithWorkMs(string workMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, workMs, FarmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithFarmMs(string farmMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, WorkMs, farmMs, LootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithLootboxMs(string lootboxMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, WorkMs, FarmMs, lootboxMs, WorkCommands);
        }

        public AppSettingsSnapshot WithWorkCommands(string workCommands)
        {
            return new AppSettingsSnapshot(ChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, WorkMs, FarmMs, LootboxMs, workCommands);
        }

        private static int ParseOrDefault(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }
    }
}
