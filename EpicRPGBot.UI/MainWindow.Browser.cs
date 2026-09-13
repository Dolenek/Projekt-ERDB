using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private bool _browserSessionsInitialized;
        private int _browserSelectionVersion;
        private DiscordWebViewSession _selectedBrowserSession;

        private async Task InitializeBrowsersAsync()
        {
            SetDiscordStatus("Initializing", "WarningBrush");
            try
            {
                SetInitHint("Initializing Discord...");
                await _botWebViewSession.SetDemandAsync(
                    DiscordWebViewActivityReason.Permanent,
                    true);
                InitHint.Visibility = Visibility.Collapsed;
                SetDiscordStatus("Ready", "SuccessBrush");
            }
            catch (Exception ex)
            {
                SetInitHint("WebView2 init failed: " + ex.Message);
                SetDiscordStatus("Error", "DangerBrush");
            }
            finally
            {
                _browserSessionsInitialized = true;
                await ReconcileSelectedBrowserSessionAsync();
                await KeepPlayerBrowserSessionActiveAsync();
            }
        }

        private async Task KeepPlayerBrowserSessionActiveAsync()
        {
            try
            {
                await _playerWebViewSession.SetDemandAsync(
                    DiscordWebViewActivityReason.Permanent,
                    true);
            }
            catch (Exception ex)
            {
                _log.Warning("[browser] Player tab permanent activation failed: " + ex.Message);
            }
        }

        private async void BrowserTabs_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!_browserSessionsInitialized || !ReferenceEquals(e.Source, BrowserTabs))
            {
                return;
            }

            await ReconcileSelectedBrowserSessionAsync();
        }

        private async Task ReconcileSelectedBrowserSessionAsync()
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
                return;
            }

            await SetSelectedDemandSafeAsync(nextSession, true);
            if (selectionVersion != _browserSelectionVersion &&
                !ReferenceEquals(nextSession, GetBrowserSession(BrowserTabs.SelectedItem as TabItem)))
            {
                await SetSelectedDemandSafeAsync(nextSession, false);
            }
        }

        private async Task SetSelectedDemandSafeAsync(
            DiscordWebViewSession session,
            bool isSelected)
        {
            try
            {
                await session.SetDemandAsync(
                    DiscordWebViewActivityReason.Selected,
                    isSelected);
            }
            catch (Exception ex)
            {
                _log.Warning("[browser] Discord tab activation failed: " + ex.Message);
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
            await NavigateBotTabAsync();
        }

        private void SelectBotTab() => SelectBrowserTab(BotBrowserTab);

        private void SelectPlayerTab() => SelectBrowserTab(PlayerBrowserTab);

        private void SelectDungeonTab() => SelectBrowserTab(DungeonBrowserTab);

        private void SelectGuildTab() => SelectBrowserTab(GuildBrowserTab);

        private async Task SelectPlayerTabAsync(CancellationToken cancellationToken)
        {
            SelectPlayerTab();
            await _playerWebViewSession.SetDemandAsync(
                DiscordWebViewActivityReason.Selected,
                true,
                cancellationToken);
        }

        private void SelectBrowserTab(TabItem tab)
        {
            if (BrowserTabs != null && tab != null)
            {
                BrowserTabs.SelectedItem = tab;
            }
        }

        private void SetInitHint(string text)
        {
            InitHint.Visibility = Visibility.Visible;
            InitHint.Text = text;
        }
    }
}
