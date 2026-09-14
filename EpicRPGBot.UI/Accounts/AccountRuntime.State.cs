using System;
using System.Threading;
using System.Windows.Controls;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.TimeCookie;

namespace EpicRPGBot.UI.Accounts
{
    public sealed partial class AccountRuntime
    {
        public BotEngine Engine { get; set; }
        public bool IsAreaTradeRunning { get; set; }
        public bool IsDungeonRunning { get; set; }
        public bool IsDuelRunning { get; set; }
        public bool DuelInitialEngineWasRunning { get; set; }
        public bool IsSleepyPotionRunning { get; set; }
        public bool IsTimeCookieRunning { get; set; }
        public bool IsWishingTokenRunning { get; set; }
        public TimeCookieTarget? ActiveTimeCookieTarget { get; set; }
        public CancellationTokenSource DungeonCancellation { get; set; }
        public CancellationTokenSource DuelCancellation { get; set; }
        public CancellationTokenSource SleepyPotionCancellation { get; set; }
        public CancellationTokenSource TimeCookieCancellation { get; set; }
        public CancellationTokenSource WishingTokenCancellation { get; set; }
        public string ActiveExclusiveBotOperation { get; set; } = string.Empty;

        public bool BrowserSessionsInitialized { get; set; }
        public int BrowserSelectionVersion { get; set; }
        public DiscordWebViewSession SelectedBrowserSession { get; set; }
        public DiscordTabRole SelectedBrowserRole { get; set; } = DiscordTabRole.Player;
        public string DiscordStatus { get; set; } = "Initializing";
        public string DiscordStatusBrushKey { get; set; } = "WarningBrush";

        public bool IsConsoleMessageNavigationRunning { get; set; }
        public SemaphoreSlim GuildWatcherSettingsGate { get; } = new SemaphoreSlim(1, 1);
        public DiscordWebViewLease GuildWatcherWebViewLease { get; set; }
        public bool GuildRaidCoordinatorStarted { get; set; }

        public int HuntCount { get; set; }
        public int AdventureCount { get; set; }
        public int TrainingCount { get; set; }
        public int WorkCount { get; set; }
        public int FarmCount { get; set; }
        public int LootboxCount { get; set; }
        public string ActivitySearchText { get; set; } = string.Empty;
        public LogKind? SelectedLogKind { get; set; }
        public int SelectedActivityPanel { get; set; }
        public bool IsActivityExpanded { get; set; } = true;
        public bool IsControlCenterExpanded { get; set; } = true;
        public Action<DiscordMessageSnapshot> PollerMessageHandler { get; set; }
        public Action<CooldownStatsSnapshot> CooldownStatsHandler { get; set; }
        public Action<string> GuildInfoHandler { get; set; }
        public Action<GuardAlertNotification> GuildGuardHandler { get; set; }
        public Action<AppSettingsSnapshot> GuildSettingsHandler { get; set; }
        public Action<AppSettingsSnapshot> AppSettingsHandler { get; set; }

        public void Dispose()
        {
            CancelOperations();
            MessagePoller.Stop();
            Engine?.Stop();
            CooldownTracker.Stop();
            GuildRaidCoordinator.Dispose();
            GuildWatcherWebViewLease = null;
            BotWebViewSession.Dispose();
            PlayerWebViewSession.Dispose();
            GuildWebViewSession.Dispose();
            DungeonWebViewSession.Dispose();
            DuelWebViewSession.Dispose();
            GuildWatcherSettingsGate.Dispose();
            DisposeCancellationSources();
            StateChanged = null;
        }

        private void CancelOperations()
        {
            DungeonCancellation?.Cancel();
            DuelCancellation?.Cancel();
            SleepyPotionCancellation?.Cancel();
            TimeCookieCancellation?.Cancel();
            WishingTokenCancellation?.Cancel();
        }

        private void DisposeCancellationSources()
        {
            DungeonCancellation?.Dispose();
            DuelCancellation?.Dispose();
            SleepyPotionCancellation?.Dispose();
            TimeCookieCancellation?.Dispose();
            WishingTokenCancellation?.Dispose();
        }
    }
}
