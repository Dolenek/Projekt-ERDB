using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.Accounts;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private bool _isConsoleMessageNavigationRunning { get => CurrentAccount.IsConsoleMessageNavigationRunning; set => CurrentAccount.IsConsoleMessageNavigationRunning = value; }

        private ConsoleMessageNavigationRouter CreateConsoleMessageNavigationRouter(AccountRuntime runtime)
        {
            var playerNavigator = (IDiscordMessageNavigator)runtime.PlayerChatClient;
            return new ConsoleMessageNavigationRouter(new List<NavigationRoute>
            {
                CreatePlayerNavigationRoute(runtime, DiscordTabRole.Bot, playerNavigator),
                CreatePlayerNavigationRoute(runtime, DiscordTabRole.Player, playerNavigator),
                CreatePlayerNavigationRoute(runtime, DiscordTabRole.Guild, playerNavigator),
                CreatePlayerNavigationRoute(runtime, DiscordTabRole.Dungeon, playerNavigator),
                CreatePlayerNavigationRoute(runtime, DiscordTabRole.Duel, playerNavigator)
            });
        }

        private NavigationRoute CreatePlayerNavigationRoute(
            AccountRuntime runtime,
            DiscordTabRole sourceTabRole,
            IDiscordMessageNavigator playerNavigator)
        {
            return new NavigationRoute(sourceTabRole,
                cancellationToken => SelectPlayerTabAsync(runtime, cancellationToken), playerNavigator);
        }

        private async void ConsoleList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var account = _activeAccountRuntime;
            using (UseAccount(account))
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
        }

        private ListBoxItem FindClickedConsoleItem(DependencyObject source)
        {
            return source == null
                ? null
                : ItemsControl.ContainerFromElement(ConsoleList, source) as ListBoxItem;
        }
    }
}
