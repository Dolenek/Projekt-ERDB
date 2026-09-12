using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private bool _isConsoleMessageNavigationRunning;

        private ConsoleMessageNavigationRouter CreateConsoleMessageNavigationRouter()
        {
            return new ConsoleMessageNavigationRouter(new List<NavigationRoute>
            {
                CreateNavigationRoute(DiscordTabRole.Bot, SelectBotTab, _botChatClient),
                CreateNavigationRoute(DiscordTabRole.Player, SelectPlayerTab, _playerChatClient),
                CreateNavigationRoute(DiscordTabRole.Guild, SelectGuildTab, _guildChatClient),
                CreateNavigationRoute(DiscordTabRole.Dungeon, SelectDungeonTab, _dungeonChatClient),
                CreateNavigationRoute(DiscordTabRole.Duel, SelectDuelTab, _duelChatClient)
            });
        }

        private static NavigationRoute CreateNavigationRoute(
            DiscordTabRole tabRole,
            System.Action selectTab,
            IDiscordChatClient chatClient)
        {
            return new NavigationRoute(tabRole, selectTab, (IDiscordMessageNavigator)chatClient);
        }

        private async void ConsoleList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var container = FindClickedConsoleItem(e.OriginalSource as DependencyObject);
            if (!(container?.DataContext is LogEntry entry) || !entry.CanNavigate ||
                _isConsoleMessageNavigationRunning)
            {
                return;
            }

            e.Handled = true;
            _isConsoleMessageNavigationRunning = true;
            try
            {
                if (!await _consoleMessageNavigationRouter.NavigateAsync(entry.MessageReference))
                {
                    _log.Warning("Linked Discord message could not be opened.");
                }
            }
            catch (System.Exception ex)
            {
                _log.Warning("Linked Discord message could not be opened: " + ex.Message);
            }
            finally
            {
                _isConsoleMessageNavigationRunning = false;
            }
        }

        private ListBoxItem FindClickedConsoleItem(DependencyObject source)
        {
            return source == null
                ? null
                : ItemsControl.ContainerFromElement(ConsoleList, source) as ListBoxItem;
        }
    }
}
