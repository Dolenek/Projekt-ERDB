using System;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Models
{
    public sealed class AppSettingsSnapshot
    {
        public AppSettingsSnapshot(
            string channelUrl,
            string dungeonListingChannelUrl,
            bool useAtMeFallback,
            string area,
            bool ascended,
            string huntMs,
            string adventureMs,
            string trainingMs,
            string workMs,
            string farmMs,
            string lootboxMs,
            string workCommands,
            string profilePlayerName,
            bool autoDeleteDungeonChannel,
            string guildRaidChannelUrl,
            string guildRaidTriggerText,
            string guildRaidMatchMode,
            string guildRaidAuthorFilter,
            CardHandSettingsSnapshot cardHand = null,
            bool bringGuardAlertsToForeground = false)
        {
            ChannelUrl = channelUrl ?? string.Empty;
            DungeonListingChannelUrl = dungeonListingChannelUrl ?? string.Empty;
            UseAtMeFallback = useAtMeFallback;
            Area = area ?? string.Empty;
            Ascended = ascended;
            HuntMs = huntMs ?? string.Empty;
            AdventureMs = adventureMs ?? string.Empty;
            TrainingMs = trainingMs ?? string.Empty;
            WorkMs = workMs ?? string.Empty;
            FarmMs = farmMs ?? string.Empty;
            LootboxMs = lootboxMs ?? string.Empty;
            WorkCommands = string.IsNullOrWhiteSpace(workCommands)
                ? AreaWorkCommandSettings.DefaultSerializedMap
                : workCommands;
            ProfilePlayerName = profilePlayerName ?? string.Empty;
            AutoDeleteDungeonChannel = autoDeleteDungeonChannel;
            GuildRaidChannelUrl = guildRaidChannelUrl ?? string.Empty;
            GuildRaidTriggerText = guildRaidTriggerText ?? string.Empty;
            GuildRaidMatchMode = NormalizeGuildRaidMatchMode(guildRaidMatchMode);
            GuildRaidAuthorFilter = guildRaidAuthorFilter ?? string.Empty;
            CardHand = cardHand ?? CardHandSettingsSnapshot.Default;
            BringGuardAlertsToForeground = bringGuardAlertsToForeground;
        }

        public string ChannelUrl { get; }

        public string DungeonListingChannelUrl { get; }

        public bool UseAtMeFallback { get; }

        public string Area { get; }

        public bool Ascended { get; }

        public string HuntMs { get; }

        public string AdventureMs { get; }

        public string TrainingMs { get; }

        public string WorkMs { get; }

        public string FarmMs { get; }

        public string LootboxMs { get; }

        public string WorkCommands { get; }

        public string ProfilePlayerName { get; }

        public bool AutoDeleteDungeonChannel { get; }

        public string GuildRaidChannelUrl { get; }

        public string GuildRaidTriggerText { get; }

        public string GuildRaidMatchMode { get; }

        public string GuildRaidAuthorFilter { get; }

        public CardHandSettingsSnapshot CardHand { get; }

        public bool BringGuardAlertsToForeground { get; }

        public static AppSettingsSnapshot Default =>
            new AppSettingsSnapshot(
                "https://discord.com/channels/@me",
                string.Empty,
                true,
                "10",
                false,
                "61000",
                "61000",
                "61000",
                "99000",
                "196000",
                "21600000",
                AreaWorkCommandSettings.DefaultSerializedMap,
                string.Empty,
                false,
                string.Empty,
                string.Empty,
                GuildRaidMatchModes.Contains,
                string.Empty,
                bringGuardAlertsToForeground: false);

        public string ResolveChannelUrl()
        {
            var url = ChannelUrl.Trim();
            if (string.IsNullOrEmpty(url) && UseAtMeFallback)
            {
                return "https://discord.com/channels/@me";
            }

            return string.IsNullOrEmpty(url) ? "https://discord.com/channels/@me" : url;
        }

        public string ResolveDungeonListingChannelUrl()
        {
            var url = DungeonListingChannelUrl.Trim();
            return string.IsNullOrEmpty(url) ? ResolveChannelUrl() : url;
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

        public int GetTrainingMsOrDefault(int defaultValue)
        {
            return ParseOrDefault(TrainingMs, defaultValue);
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

        public string ResolveGuildRaidChannelUrl()
        {
            return GuildRaidChannelUrl.Trim();
        }

        public string ResolveGuildRaidTriggerText()
        {
            return GuildRaidTriggerText.Trim();
        }

        public string ResolveGuildRaidAuthorFilter()
        {
            return GuildRaidAuthorFilter.Trim();
        }

        public bool IsGuildRaidConfigured()
        {
            return !string.IsNullOrWhiteSpace(ResolveGuildRaidChannelUrl()) &&
                   !string.IsNullOrWhiteSpace(ResolveGuildRaidTriggerText());
        }

        public bool TryResolveGuildRaidChannelUrl(out string channelUrl)
        {
            channelUrl = ResolveGuildRaidChannelUrl();
            if (string.IsNullOrWhiteSpace(channelUrl))
            {
                return false;
            }

            if (!Uri.TryCreate(channelUrl, UriKind.Absolute, out var parsedUri))
            {
                channelUrl = string.Empty;
                return false;
            }

            var pathSegments = parsedUri.AbsolutePath.Trim('/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var isDiscordChannelUrl =
                string.Equals(parsedUri.Host, "discord.com", StringComparison.OrdinalIgnoreCase) &&
                pathSegments.Length >= 3 &&
                string.Equals(pathSegments[0], "channels", StringComparison.OrdinalIgnoreCase);
            if (!isDiscordChannelUrl)
            {
                channelUrl = string.Empty;
                return false;
            }

            return true;
        }

        public bool UsesExactGuildRaidMatch()
        {
            return string.Equals(GuildRaidMatchMode, GuildRaidMatchModes.Exact, StringComparison.Ordinal);
        }

        public AppSettingsSnapshot WithChannelUrl(string channelUrl)
        {
            return new AppSettingsSnapshot(channelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithDungeonListingChannelUrl(string dungeonListingChannelUrl)
        {
            return new AppSettingsSnapshot(ChannelUrl, dungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithUseAtMeFallback(bool useAtMeFallback)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, useAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithArea(string area)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithAscended(bool ascended)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithHuntMs(string huntMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, huntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithAdventureMs(string adventureMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, adventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithTrainingMs(string trainingMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, trainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithWorkMs(string workMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, workMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithFarmMs(string farmMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, farmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithLootboxMs(string lootboxMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, lootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithWorkCommands(string workCommands)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, workCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithProfilePlayerName(string profilePlayerName)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, profilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithAutoDeleteDungeonChannel(bool autoDeleteDungeonChannel)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, autoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithGuildRaidChannelUrl(string guildRaidChannelUrl)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, guildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithGuildRaidTriggerText(string guildRaidTriggerText)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, guildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithGuildRaidMatchMode(string guildRaidMatchMode)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, guildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithGuildRaidAuthorFilter(string guildRaidAuthorFilter)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, guildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithCardHand(CardHandSettingsSnapshot cardHand)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, cardHand, BringGuardAlertsToForeground);
        }

        public AppSettingsSnapshot WithBringGuardAlertsToForeground(bool bringGuardAlertsToForeground)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, bringGuardAlertsToForeground);
        }

        private static int ParseOrDefault(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private static string NormalizeGuildRaidMatchMode(string value)
        {
            return string.Equals(value?.Trim(), GuildRaidMatchModes.Exact, StringComparison.OrdinalIgnoreCase)
                ? GuildRaidMatchModes.Exact
                : GuildRaidMatchModes.Contains;
        }
    }
}
