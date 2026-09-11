using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private ICollectionView _lastMessagesView;
        private ICollectionView _consoleView;

        private void BindActivityUi()
        {
            _lastMessagesView = CollectionViewSource.GetDefaultView(_last.Items);
            _consoleView = CollectionViewSource.GetDefaultView(_log.Items);
            _lastMessagesView.Filter = item => ActivityEntryFilter.MatchesMessage(
                item as MessageItem,
                ActivitySearchBox?.Text);
            _consoleView.Filter = item => ActivityEntryFilter.MatchesLog(
                item as LogEntry,
                ActivitySearchBox?.Text,
                GetSelectedLogKind());
            StatsList.ItemsSource = _lastMessagesView;
            ConsoleList.ItemsSource = _consoleView;
        }

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
            ActivityFilterPanel.Visibility = statsVisible ? Visibility.Collapsed : Visibility.Visible;
            ActivityKindFilter.Visibility = consoleVisible ? Visibility.Visible : Visibility.Collapsed;
            LastMessagesTabBtn.Tag = lastMessagesVisible ? "Selected" : null;
            StatsTabBtn.Tag = statsVisible ? "Selected" : null;
            ConsoleTabBtn.Tag = consoleVisible ? "Selected" : null;
            RefreshActivityViews();
        }

        private void ActivitySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ActivitySearchPlaceholder != null)
            {
                ActivitySearchPlaceholder.Visibility = string.IsNullOrEmpty(ActivitySearchBox.Text)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            RefreshActivityViews();
        }

        private void ActivityKindFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshActivityViews();
        }

        private LogKind? GetSelectedLogKind()
        {
            var selectedItem = ActivityKindFilter?.SelectedItem as ComboBoxItem;
            return selectedItem?.Tag is LogKind selectedKind ? selectedKind : (LogKind?)null;
        }

        private void RefreshActivityViews()
        {
            _lastMessagesView?.Refresh();
            _consoleView?.Refresh();
        }
    }
}
