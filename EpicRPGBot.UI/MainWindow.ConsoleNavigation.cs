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
            var playerNavigator = (IDiscordMessageNavigator)_playerChatClient;
            return new ConsoleMessageNavigationRouter(new List<NavigationRoute>
            {
                CreatePlayerNavigationRoute(DiscordTabRole.Bot, playerNavigator),
                CreatePlayerNavigationRoute(DiscordTabRole.Player, playerNavigator),
                CreatePlayerNavigationRoute(DiscordTabRole.Guild, playerNavigator),
                CreatePlayerNavigationRoute(DiscordTabRole.Dungeon, playerNavigator),
                CreatePlayerNavigationRoute(DiscordTabRole.Duel, playerNavigator)
            });
        }

        private NavigationRoute CreatePlayerNavigationRoute(
            DiscordTabRole sourceTabRole,
            IDiscordMessageNavigator playerNavigator)
        {
            return new NavigationRoute(sourceTabRole, SelectPlayerTab, playerNavigator);
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
