using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async Task InitializeBrowsersAsync()
        {
            try
            {
                SetInitHint("Initializing Discord tabs...");
                await WarmUpBrowserTabsAsync();
                await _botChatClient.EnsureInitializedAsync();
                await _playerChatClient.EnsureInitializedAsync();
                await _guildChatClient.EnsureInitializedAsync();
                await _dungeonChatClient.EnsureInitializedAsync();
                InitHint.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                SetInitHint("WebView2 init failed: " + ex.Message);
            }
        }

        private async Task WarmUpBrowserTabsAsync()
        {
            if (BrowserTabs == null || BotBrowserTab == null || PlayerBrowserTab == null || GuildBrowserTab == null || DungeonBrowserTab == null)
            {
                return;
            }

            var originalSelection = BrowserTabs.SelectedItem;
            await ShowTabAsync(BotBrowserTab);
            await ShowTabAsync(PlayerBrowserTab);
            await ShowTabAsync(GuildBrowserTab);
            await ShowTabAsync(DungeonBrowserTab);
            BrowserTabs.SelectedItem = originalSelection ?? BotBrowserTab;
            BrowserTabs.UpdateLayout();
        }

        private async Task ShowTabAsync(TabItem tab)
        {
            BrowserTabs.SelectedItem = tab;
            BrowserTabs.UpdateLayout();
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        }

        private async Task NavigateStartupTabsAsync()
        {
            try
            {
                var channelUrl = GetChannelUrl();
                await _botChatClient.NavigateToChannelAsync(channelUrl);
                await _playerChatClient.NavigateToChannelAsync(channelUrl);
                await _dungeonChatClient.NavigateToChannelAsync(channelUrl);
            }
            catch (Exception ex)
            {
                SetInitHint("Navigate failed: " + ex.Message);
            }
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
            try
            {
                _botChatClient.Reload();
            }
            catch
            {
            }
        }

        private async void GoChannelBtn_Click(object sender, RoutedEventArgs e)
        {
            await NavigateBotTabAsync();
        }

        private void SelectBotTab()
        {
            SelectBrowserTab(BotBrowserTab);
        }

        private void SelectDungeonTab()
        {
            SelectBrowserTab(DungeonBrowserTab);
        }

        private void SelectGuildTab()
        {
            SelectBrowserTab(GuildBrowserTab);
        }

        private void SelectBrowserTab(TabItem tab)
        {
            if (BrowserTabs == null || tab == null)
            {
                return;
            }

            BrowserTabs.SelectedItem = tab;
        }

        private void SetInitHint(string text)
        {
            InitHint.Visibility = Visibility.Visible;
            InitHint.Text = text;
        }
    }
}
