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
            SetAutomationIdentity(TrainingCountText, "TrainingCountStat");
            SetAutomationIdentity(WorkCountText, "WorkCountStat");
            SetAutomationIdentity(FarmCountText, "FarmCountStat");
            SetAutomationIdentity(LootboxCountText, "LootboxCountStat");
            SetAutomationIdentity(RunningCooldownsText, "RunningCooldownsStat");
            SetAutomationIdentity(RunningRewardsText, "RunningRewardsStat");
            SetAutomationIdentity(RunningExperienceText, "RunningExperienceStat");
            SetAutomationIdentity(RunningProgressText, "RunningProgressStat");
            SetAutomationIdentity(ConsoleList, "ConsoleList");
            SetAutomationIdentity(ActivitySearchBox, "ActivitySearchInput");
            SetAutomationIdentity(ActivityKindFilter, "ActivityKindFilter");
            SetAutomationIdentity(ActivityCollapseBtn, "ActivityCollapseButton");
            SetAutomationIdentity(ActivityExpandBtn, "ActivityExpandButton");
            SetAutomationIdentity(DismantleBtn, "DismantleButton");
            SetAutomationIdentity(CraftingBtn, "CraftingButton");
            SetAutomationIdentity(SettingsBtn, "SettingsButton");
            SetAutomationIdentity(ReloadBtn, "ReloadButton");
            SetAutomationIdentity(GoChannelBtn, "GoChannelButton");
            SetAutomationIdentity(BrowserTabs, "BrowserTabs");
            SetAutomationIdentity(BotBrowserTab, "BotBrowserTab");
            SetAutomationIdentity(PlayerBrowserTab, "PlayerBrowserTab");
            SetAutomationIdentity(GuildBrowserTab, "GuildBrowserTab");
            SetAutomationIdentity(DungeonBrowserTab, "DungeonBrowserTab");
            SetAutomationIdentity(DuelBrowserTab, "DuelBrowserTab");
            SetAutomationIdentity(Web, "DiscordWebView");
            SetAutomationIdentity(PlayerWeb, "PlayerDiscordWebView");
            SetAutomationIdentity(GuildWeb, "GuildDiscordWebView");
            SetAutomationIdentity(DungeonWeb, "DungeonDiscordWebView");
            SetAutomationIdentity(DuelWeb, "DuelDiscordWebView");
            SetAutomationIdentity(InitHint, "InitHint");
            SetAutomationIdentity(ConnectionStatusText, "DiscordStatusText");
            SetAutomationIdentity(EngineStatusText, "EngineStatusText");
            SetAutomationIdentity(StartBtn, "StartButton");
            SetAutomationIdentity(StopBtn, "StopButton");
            SetAutomationIdentity(InitBtn, "InitializeButton");
            SetAutomationIdentity(RpgCdBtn, "RpgCdButton");
            SetAutomationIdentity(TradeAreaBtn, "TradeAreaButton");
            SetAutomationIdentity(WishingTokenBtn, "WishingTokenButton");
            SetAutomationIdentity(CompleteDungeonBtn, "CompleteDungeonButton");
            SetAutomationIdentity(DuelBtn, "DuelButton");
            SetAutomationIdentity(SleepyPotionBtn, "SleepyPotionButton");
            SetAutomationIdentity(TimeCookieDungeonBtn, "TimeCookieDungeonButton");
            SetAutomationIdentity(TimeCookieDuelBtn, "TimeCookieDuelButton");
            SetAutomationIdentity(TimeCookieCardHandBtn, "TimeCookieCardHandButton");
            SetAutomationIdentity(ControlCenterCollapseBtn, "ControlCenterCollapseButton");
            SetAutomationIdentity(ControlCenterExpandBtn, "ControlCenterExpandButton");
            SetAutomationIdentity(MinimizeBtn, "MinimizeButton");
            SetAutomationIdentity(MaximizeRestoreBtn, "MaximizeRestoreButton");
            SetAutomationIdentity(CloseWindowBtn, "CloseWindowButton");
            SetAutomationIdentity(CooldownVisual, "CooldownPanelControl");
            CooldownVisual?.ApplyAutomationIds();

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
