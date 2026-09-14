using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EpicRPGBot.UI.Accounts;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private bool _browserSessionsInitialized { get => CurrentAccount.BrowserSessionsInitialized; set => CurrentAccount.BrowserSessionsInitialized = value; }
        private int _browserSelectionVersion { get => CurrentAccount.BrowserSelectionVersion; set => CurrentAccount.BrowserSelectionVersion = value; }
        private DiscordWebViewSession _selectedBrowserSession { get => CurrentAccount.SelectedBrowserSession; set => CurrentAccount.SelectedBrowserSession = value; }

        private async Task InitializeBrowsersAsync()
        {
            SetDiscordStatus("Initializing", "WarningBrush");
            try
            {
                SetInitHint("Initializing Discord...");
                _browserSessionsInitialized = true;
                if (await ReconcileSelectedBrowserSessionAsync())
                {
                    InitHint.Visibility = Visibility.Collapsed;
                    SetDiscordStatus("Ready", "SuccessBrush");
                }
            }
            catch (Exception ex)
            {
                SetInitHint("WebView2 init failed: " + ex.Message);
                SetDiscordStatus("Error", "DangerBrush");
            }
            finally
            {
                _browserSessionsInitialized = true;
            }
        }

        private async void BrowserTabs_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_activeAccountRuntime == null || !_browserSessionsInitialized || !ReferenceEquals(e.Source, BrowserTabs))
            {
                return;
            }

            var account = _activeAccountRuntime;
            using (UseAccount(account))
            {
                CurrentAccount.SelectedBrowserRole = GetBrowserRole(BrowserTabs.SelectedItem as TabItem);
                await ReconcileSelectedBrowserSessionAsync();
            }
        }

        private async Task<bool> ReconcileSelectedBrowserSessionAsync()
        {
            var selectionVersion = ++_browserSelectionVersion;
            var nextSession = GetBrowserSession(BrowserTabs.SelectedItem as TabItem);
            var previousSession = _selectedBrowserSession;
            _selectedBrowserSession = nextSession;

            if (previousSession != null && !ReferenceEquals(previousSession, nextSession))
            {
                await SetSelectedDemandSafeAsync(previousSession, false);
            }

            if (nextSession == null)
            {
                return false;
            }

            if (!await SetSelectedDemandSafeAsync(nextSession, true)) return false;
            ReconcileMessagePolling();
            if (!ReferenceEquals(CurrentAccount, _activeAccountRuntime))
            {
                await SetSelectedDemandSafeAsync(nextSession, false);
                return false;
            }
            if (selectionVersion != _browserSelectionVersion &&
                !ReferenceEquals(nextSession, GetBrowserSession(BrowserTabs.SelectedItem as TabItem)))
            {
                await SetSelectedDemandSafeAsync(nextSession, false);
                return false;
            }

            SetDiscordStatus("Ready", "SuccessBrush");
            if (ReferenceEquals(CurrentAccount, _activeAccountRuntime))
                InitHint.Visibility = Visibility.Collapsed;
            return true;
        }

        private async Task<bool> SetSelectedDemandSafeAsync(
            DiscordWebViewSession session,
            bool isSelected)
        {
            try
            {
                await session.SetDemandAsync(
                    DiscordWebViewActivityReason.Selected,
                    isSelected);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warning("[browser] Discord tab activation failed: " + ex.Message);
                if (isSelected)
                {
                    SetDiscordStatus("Error", "DangerBrush");
                    if (ReferenceEquals(CurrentAccount, _activeAccountRuntime))
                        SetInitHint("WebView2 init failed: " + ex.Message);
                }
                return false;
            }
        }

        private DiscordWebViewSession GetBrowserSession(TabItem tab)
        {
            if (ReferenceEquals(tab, BotBrowserTab)) return _botWebViewSession;
            if (ReferenceEquals(tab, PlayerBrowserTab)) return _playerWebViewSession;
            if (ReferenceEquals(tab, GuildBrowserTab)) return _guildWebViewSession;
            if (ReferenceEquals(tab, DungeonBrowserTab)) return _dungeonWebViewSession;
            if (ReferenceEquals(tab, DuelBrowserTab)) return _duelWebViewSession;
            return null;
        }

        private DiscordTabRole GetBrowserRole(TabItem tab)
        {
            if (ReferenceEquals(tab, BotBrowserTab)) return DiscordTabRole.Bot;
            if (ReferenceEquals(tab, GuildBrowserTab)) return DiscordTabRole.Guild;
            if (ReferenceEquals(tab, DungeonBrowserTab)) return DiscordTabRole.Dungeon;
            if (ReferenceEquals(tab, DuelBrowserTab)) return DiscordTabRole.Duel;
            return DiscordTabRole.Player;
        }

        private TabItem GetBrowserTab(DiscordTabRole role)
        {
            switch (role)
            {
                case DiscordTabRole.Bot: return BotBrowserTab;
                case DiscordTabRole.Guild: return GuildBrowserTab;
                case DiscordTabRole.Dungeon: return DungeonBrowserTab;
                case DiscordTabRole.Duel: return DuelBrowserTab;
                default: return PlayerBrowserTab;
            }
        }

        private string GetGuildInitialUrl()
        {
            return GetCurrentSettings().TryResolveGuildRaidChannelUrl(out var channelUrl)
                ? channelUrl
                : GetChannelUrl();
        }

        private string GetDungeonInitialUrl()
        {
            return GetCurrentSettings().ResolveDungeonListingChannelUrl();
        }

        private async Task NavigateBotTabAsync()
        {
            try
            {
                await _botChatClient.NavigateToChannelAsync(GetChannelUrl());
            }
            catch (Exception ex)
            {
                SetInitHint("Navigate failed: " + ex.Message);
            }
        }

        private string GetChannelUrl()
        {
            return GetCurrentSettings().ResolveChannelUrl();
        }

        private void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try { _botChatClient.Reload(); }
            catch { }
        }

        private async void GoChannelBtn_Click(object sender, RoutedEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account)) await NavigateBotTabAsync();
        }

        private void SelectBotTab() => SelectBrowserTab(BotBrowserTab);

        private void SelectPlayerTab() => SelectBrowserTab(PlayerBrowserTab);

        private void SelectDungeonTab() => SelectBrowserTab(DungeonBrowserTab);

        private void SelectGuildTab() => SelectBrowserTab(GuildBrowserTab);

        private async Task SelectPlayerTabAsync(AccountRuntime runtime, CancellationToken cancellationToken)
        {
            await ActivateAccountAsync(runtime);
            using (UseAccount(runtime))
            {
                SelectPlayerTab();
                await _playerWebViewSession.SetDemandAsync(
                    DiscordWebViewActivityReason.Selected, true, cancellationToken);
            }
        }

        private async Task ActivateAccountAndSelectTabAsync(
            AccountRuntime runtime,
            DiscordTabRole role)
        {
            await ActivateAccountAsync(runtime);
            using (UseAccount(runtime))
            {
                SelectBrowserTab(GetBrowserTab(role));
            }
        }

        private void SelectBrowserTab(TabItem tab)
        {
            if (tab == null) return;
            CurrentAccount.SelectedBrowserRole = GetBrowserRole(tab);
            if (ReferenceEquals(CurrentAccount, _activeAccountRuntime) && BrowserTabs != null)
            {
                BrowserTabs.SelectedItem = tab;
            }
        }

        private async Task SetEngineBrowserDemandAsync(bool isRequired)
        {
            try
            {
                await _botWebViewSession.SetDemandAsync(
                    DiscordWebViewActivityReason.Engine, isRequired);
                ReconcileMessagePolling();
            }
            catch (Exception ex)
            {
                _log.Warning("[browser] Bot activation failed: " + ex.Message);
                throw;
            }
        }

        private Task<DiscordWebViewLease> AcquireBotWorkflowAsync()
        {
            return _botWebViewSession.AcquireAsync(DiscordWebViewActivityReason.Workflow);
        }

        private async Task ReleaseStoppedEngineDemandAsync()
        {
            if (_engine?.IsRunning != true)
                await SetEngineBrowserDemandAsync(false);
        }

        private void ReconcileMessagePolling()
        {
            var shouldPoll = _engine?.IsRunning == true ||
                ReferenceEquals(_selectedBrowserSession, _botWebViewSession);
            if (shouldPoll && _botChatClient.IsReady) _messagePoller.Start();
            else _messagePoller.Stop();
        }

        private void SetInitHint(string text)
        {
            InitHint.Visibility = Visibility.Visible;
            InitHint.Text = text;
        }
    }
}
