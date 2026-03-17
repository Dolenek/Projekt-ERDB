using System.Windows.Automation;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "MainWindow");
            SetAutomationIdentity(LastMessagesTabBtn, "LastMessagesTab");
            SetAutomationIdentity(StatsTabBtn, "StatsTab");
            SetAutomationIdentity(ConsoleTabBtn, "ConsoleTab");
            SetAutomationIdentity(StatsList, "LastMessagesList");
            SetAutomationIdentity(HuntCountText, "HuntCountStat");
            SetAutomationIdentity(AdventureCountText, "AdventureCountStat");
            SetAutomationIdentity(WorkCountText, "WorkCountStat");
            SetAutomationIdentity(FarmCountText, "FarmCountStat");
            SetAutomationIdentity(LootboxCountText, "LootboxCountStat");
            SetAutomationIdentity(RunningCooldownsText, "RunningCooldownsStat");
            SetAutomationIdentity(RunningRewardsText, "RunningRewardsStat");
            SetAutomationIdentity(RunningExperienceText, "RunningExperienceStat");
            SetAutomationIdentity(RunningProgressText, "RunningProgressStat");
            SetAutomationIdentity(ConsoleList, "ConsoleList");
            SetAutomationIdentity(DismantleBtn, "DismantleButton");
            SetAutomationIdentity(CraftingBtn, "CraftingButton");
            SetAutomationIdentity(SettingsBtn, "SettingsButton");
            SetAutomationIdentity(ReloadBtn, "ReloadButton");
            SetAutomationIdentity(GoChannelBtn, "GoChannelButton");
            SetAutomationIdentity(BrowserTabs, "BrowserTabs");
            SetAutomationIdentity(BotBrowserTab, "BotBrowserTab");
            SetAutomationIdentity(PlayerBrowserTab, "PlayerBrowserTab");
            SetAutomationIdentity(Web, "DiscordWebView");
            SetAutomationIdentity(PlayerWeb, "PlayerDiscordWebView");
            SetAutomationIdentity(InitHint, "InitHint");
            SetAutomationIdentity(StartBtn, "StartButton");
            SetAutomationIdentity(StopBtn, "StopButton");
            SetAutomationIdentity(InitBtn, "InitializeButton");
            SetAutomationIdentity(RpgCdBtn, "RpgCdButton");
            SetAutomationIdentity(TradeAreaBtn, "TradeAreaButton");
            SetAutomationIdentity(CooldownsPanel, "CooldownsPanel");

            if (!Automation.AutomationRuntime.Current.IsEnabled)
            {
                return;
            }

            Title = string.IsNullOrWhiteSpace(Automation.AutomationRuntime.Current.SessionId)
                ? "EpicRPG Bot UI [Automation]"
                : $"EpicRPG Bot UI [Automation {Automation.AutomationRuntime.Current.SessionId}]";
        }

        private static void SetAutomationIdentity(System.Windows.DependencyObject element, string automationId)
        {
            if (element == null || string.IsNullOrWhiteSpace(automationId))
            {
                return;
            }

            AutomationProperties.SetAutomationId(element, automationId);
        }
    }
}
