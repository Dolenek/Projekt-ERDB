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
            SetAutomationIdentity(ConsoleList, "ConsoleList");
            SetAutomationIdentity(ReloadBtn, "ReloadButton");
            SetAutomationIdentity(GoChannelBtn, "GoChannelButton");
            SetAutomationIdentity(BrowserTabs, "BrowserTabs");
            SetAutomationIdentity(BotBrowserTab, "BotBrowserTab");
            SetAutomationIdentity(PlayerBrowserTab, "PlayerBrowserTab");
            SetAutomationIdentity(Web, "DiscordWebView");
            SetAutomationIdentity(PlayerWeb, "PlayerDiscordWebView");
            SetAutomationIdentity(InitHint, "InitHint");
            SetAutomationIdentity(ChannelUrlBox, "ChannelUrlInput");
            SetAutomationIdentity(UseAtMeFallback, "UseAtMeFallback");
            SetAutomationIdentity(AreaBox, "AreaInput");
            SetAutomationIdentity(HuntCdBox, "HuntCooldownInput");
            SetAutomationIdentity(AdventureCdBox, "AdventureCooldownInput");
            SetAutomationIdentity(WorkCdBox, "WorkCooldownInput");
            SetAutomationIdentity(FarmCdBox, "FarmCooldownInput");
            SetAutomationIdentity(LootboxCdBox, "LootboxCooldownInput");
            SetAutomationIdentity(StartBtn, "StartButton");
            SetAutomationIdentity(StopBtn, "StopButton");
            SetAutomationIdentity(InitBtn, "InitializeButton");
            SetAutomationIdentity(RpgCdBtn, "RpgCdButton");
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
