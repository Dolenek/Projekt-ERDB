using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void LastMessagesTabBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowSidebarPanel(lastMessagesVisible: true, statsVisible: false, consoleVisible: false);
        }

        private void StatsTabBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowSidebarPanel(lastMessagesVisible: false, statsVisible: true, consoleVisible: false);
        }

        private void ConsoleTabBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowSidebarPanel(lastMessagesVisible: false, statsVisible: false, consoleVisible: true);
        }

        private void ShowSidebarPanel(bool lastMessagesVisible, bool statsVisible, bool consoleVisible)
        {
            if (_lastMessagesPanel != null)
            {
                _lastMessagesPanel.Visibility = lastMessagesVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            StatsPanel.Visibility = statsVisible ? Visibility.Visible : Visibility.Collapsed;
            ConsolePanel.Visibility = consoleVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
