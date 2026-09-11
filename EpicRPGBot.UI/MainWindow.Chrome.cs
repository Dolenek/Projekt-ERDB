using System;
using System.Windows;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const double ExpandedActivityWidth = 300;
        private const double ExpandedControlCenterWidth = 360;
        private const double CollapsedPanelWidth = 36;
        private bool _isActivityExpanded = true;
        private bool _isControlCenterExpanded = true;

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            WindowWorkAreaChrome.Attach(this);
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeRestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
                return;
            }

            SystemCommands.MaximizeWindow(this);
        }

        private void CloseWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void MainWindow_StateChanged(object sender, EventArgs e)
        {
            if (MaximizeRestoreBtn != null)
            {
                MaximizeRestoreBtn.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
            }
        }

        private void ActivityToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            _isActivityExpanded = !_isActivityExpanded;
            ActivityColumn.Width = new GridLength(
                _isActivityExpanded ? ExpandedActivityWidth : CollapsedPanelWidth);
            ActivityExpandedContent.Visibility = _isActivityExpanded
                ? Visibility.Visible
                : Visibility.Collapsed;
            ActivityCollapsedContent.Visibility = _isActivityExpanded
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void ControlCenterToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            _isControlCenterExpanded = !_isControlCenterExpanded;
            ControlCenterColumn.Width = new GridLength(
                _isControlCenterExpanded ? ExpandedControlCenterWidth : CollapsedPanelWidth);
            ControlCenterExpandedContent.Visibility = _isControlCenterExpanded
                ? Visibility.Visible
                : Visibility.Collapsed;
            ControlCenterCollapsedContent.Visibility = _isControlCenterExpanded
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}
