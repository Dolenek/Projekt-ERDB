using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using EpicRPGBot.UI.Models;
using Forms = System.Windows.Forms;

namespace EpicRPGBot.UI.Services
{
    public sealed class DesktopAlertService : IDisposable
    {
        private const int SwRestore = 9;
        private readonly Forms.NotifyIcon _notifyIcon;

        public DesktopAlertService()
        {
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = SystemIcons.Warning,
                Visible = true,
                Text = "EpicRPGBot.UI"
            };
        }

        public void ShowGuardAlert(Window window, GuardAlertNotification notification)
        {
            if (notification == null)
            {
                return;
            }

            if (notification.ShouldPlaySound)
            {
                PlayAlertSound();
            }

            if (notification.ShouldShowBalloon)
            {
                ShowBalloon(notification);
            }

            if (notification.ShouldBringToFront)
            {
                BringToFront(window);
            }
        }

        public void ShowTrainingAlert(Window window, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ShowWarningBalloon("Training prompt skipped", message);
        }

        public void ShowBunnyAlert(Window window, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            ShowWarningBalloon("Pet prompt issue", message);
        }

        private static void PlayAlertSound()
        {
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
            }

        }

        private void ShowBalloon(GuardAlertNotification notification)
        {
            var title = notification.Kind == GuardAlertKind.Reminder
                ? "EPIC GUARD still active"
                : "EPIC GUARD detected";
            var message = notification.Kind == GuardAlertKind.Reminder
                ? "Captcha check is still active. Review the bot window when available."
                : "Captcha check detected. Review the bot window now.";

            try
            {
                _notifyIcon.ShowBalloonTip(5000, title, message, Forms.ToolTipIcon.Warning);
            }
            catch
            {
            }
        }

        private void ShowWarningBalloon(string title, string message)
        {
            try
            {
                _notifyIcon.ShowBalloonTip(5000, title, message, Forms.ToolTipIcon.Warning);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        private static void BringToFront(Window window)
        {
            if (window == null)
            {
                return;
            }

            UiDispatcher.OnUI(() =>
            {
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }

                window.Show();
                window.Topmost = true;
                window.Activate();
                window.Topmost = false;
                window.Focus();

                var handle = new WindowInteropHelper(window).Handle;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                ShowWindow(handle, SwRestore);
                SetForegroundWindow(handle);
            });
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
