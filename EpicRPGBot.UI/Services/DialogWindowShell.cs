using System;
using System.Windows;
using System.Windows.Controls;

namespace EpicRPGBot.UI.Services
{
    internal static class DialogWindowShell
    {
        public static void Attach(
            Window window,
            Button minimizeButton,
            Button maximizeRestoreButton,
            Button closeButton)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (minimizeButton == null) throw new ArgumentNullException(nameof(minimizeButton));
            if (maximizeRestoreButton == null) throw new ArgumentNullException(nameof(maximizeRestoreButton));
            if (closeButton == null) throw new ArgumentNullException(nameof(closeButton));

            window.SourceInitialized += (sender, args) => WindowWorkAreaChrome.Attach(window);
            window.Loaded += (sender, args) => WindowWorkAreaChrome.ConstrainToWorkArea(window);
            window.StateChanged += (sender, args) => UpdateMaximizeGlyph(window, maximizeRestoreButton);
            minimizeButton.Click += (sender, args) => SystemCommands.MinimizeWindow(window);
            maximizeRestoreButton.Click += (sender, args) => ToggleMaximized(window);
            closeButton.Click += (sender, args) => SystemCommands.CloseWindow(window);
            UpdateMaximizeGlyph(window, maximizeRestoreButton);
        }

        private static void ToggleMaximized(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(window);
                return;
            }

            SystemCommands.MaximizeWindow(window);
        }

        private static void UpdateMaximizeGlyph(Window window, ContentControl maximizeRestoreButton)
        {
            maximizeRestoreButton.Content = window.WindowState == WindowState.Maximized
                ? "\uE923"
                : "\uE922";
        }
    }
}
