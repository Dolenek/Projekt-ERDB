using System;
using System.Windows;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public partial class SettingsWindow
    {
        private void SettingsWindow_SourceInitialized(object sender, EventArgs e)
        {
            WindowWorkAreaChrome.Attach(this);
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowWorkAreaChrome.ConstrainToWorkArea(this);
        }

        private void SettingsMinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void SettingsMaximizeRestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
                return;
            }

            SystemCommands.MaximizeWindow(this);
        }

        private void SettingsCloseWindowBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void SettingsWindow_StateChanged(object sender, EventArgs e)
        {
            if (SettingsMaximizeRestoreBtn != null)
            {
                SettingsMaximizeRestoreBtn.Content = WindowState == WindowState.Maximized
                    ? "\uE923"
                    : "\uE922";
            }
        }
    }
}
