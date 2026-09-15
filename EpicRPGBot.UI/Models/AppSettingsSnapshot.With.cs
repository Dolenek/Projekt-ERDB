using EpicRPGBot.UI.CardHand;

namespace EpicRPGBot.UI.Models
{
    public sealed partial class AppSettingsSnapshot
    {
        public AppSettingsSnapshot WithChannelUrl(string channelUrl)
        {
            return new AppSettingsSnapshot(channelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithDungeonListingChannelUrl(string dungeonListingChannelUrl)
        {
            return new AppSettingsSnapshot(ChannelUrl, dungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithUseAtMeFallback(bool useAtMeFallback)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, useAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithArea(string area)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithAscended(bool ascended)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithHuntMs(string huntMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, huntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithAdventureMs(string adventureMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, adventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithTrainingMs(string trainingMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, trainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithWorkMs(string workMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, workMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithFarmMs(string farmMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, farmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithLootboxMs(string lootboxMs)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, lootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithWorkCommands(string workCommands)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, workCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithProfilePlayerName(string profilePlayerName)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, profilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithAutoDeleteDungeonChannel(bool autoDeleteDungeonChannel)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, autoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithGuildRaidChannelUrl(string guildRaidChannelUrl)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, guildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithGuildRaidTriggerText(string guildRaidTriggerText)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, guildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithGuildRaidMatchMode(string guildRaidMatchMode)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, guildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithGuildRaidAuthorFilter(string guildRaidAuthorFilter)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, guildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithCardHand(CardHandSettingsSnapshot cardHand)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, cardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithBringGuardAlertsToForeground(bool bringGuardAlertsToForeground)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, bringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithGuildRaidWatcherActive(bool guildRaidWatcherActive)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, guildRaidWatcherActive, UseHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithHardcoreHuntAndAdventure(bool useHardcoreHuntAndAdventure)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, useHardcoreHuntAndAdventure, HealAfterHuntAndAdventure);
        }

        public AppSettingsSnapshot WithHealAfterHuntAndAdventure(bool healAfterHuntAndAdventure)
        {
            return new AppSettingsSnapshot(ChannelUrl, DungeonListingChannelUrl, UseAtMeFallback, Area, Ascended, HuntMs, AdventureMs, TrainingMs, WorkMs, FarmMs, LootboxMs, WorkCommands, ProfilePlayerName, AutoDeleteDungeonChannel, GuildRaidChannelUrl, GuildRaidTriggerText, GuildRaidMatchMode, GuildRaidAuthorFilter, CardHand, BringGuardAlertsToForeground, GuildRaidWatcherActive, UseHardcoreHuntAndAdventure, healAfterHuntAndAdventure);
        }
    }
}
