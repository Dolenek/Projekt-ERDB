using System;
using System.Windows;
using System.Windows.Automation;

namespace EpicRPGBot.UI.Settings
{
    public partial class SettingsWindow
    {
        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "SettingsWindow");
            SetAutomationIdentity(ChannelUrlBox, "SettingsChannelUrlInput");
            SetAutomationIdentity(DungeonListingChannelUrlBox, "SettingsDungeonListingChannelUrlInput");
            SetAutomationIdentity(UseAtMeFallback, "SettingsUseAtMeFallback");
            SetAutomationIdentity(AreaBox, "SettingsAreaInput");
            SetAutomationIdentity(AscendedCheckBox, "SettingsAscendedInput");
            SetAutomationIdentity(AutoDeleteDungeonChannelCheckBox, "SettingsAutoDeleteDungeonChannelInput");
            SetAutomationIdentity(HuntCdBox, "SettingsHuntCooldownInput");
            SetAutomationIdentity(AdventureCdBox, "SettingsAdventureCooldownInput");
            SetAutomationIdentity(TrainingCdBox, "SettingsTrainingCooldownInput");
            SetAutomationIdentity(WorkCdBox, "SettingsWorkCooldownInput");
            SetAutomationIdentity(FarmCdBox, "SettingsFarmCooldownInput");
            SetAutomationIdentity(LootboxCdBox, "SettingsLootboxCooldownInput");
            SetAutomationIdentity(WorkCommandsBtn, "SettingsWorkCommandsButton");
            SetAutomationIdentity(GuildRaidBtn, "SettingsGuildRaidButton");
            SetAutomationIdentity(CardHandBtn, "SettingsCardHandButton");
            SetAutomationIdentity(CloseBtn, "SettingsCloseButton");
            SetAutomationIdentity(SettingsMinimizeBtn, "SettingsMinimizeButton");
            SetAutomationIdentity(SettingsMaximizeRestoreBtn, "SettingsMaximizeRestoreButton");
            SetAutomationIdentity(SettingsCloseWindowBtn, "SettingsWindowCloseButton");
        }

        private static void SetAutomationIdentity(DependencyObject element, string automationId)
        {
            if (element == null || string.IsNullOrWhiteSpace(automationId))
            {
                return;
            }

            AutomationProperties.SetAutomationId(element, automationId);
        }
    }
}
