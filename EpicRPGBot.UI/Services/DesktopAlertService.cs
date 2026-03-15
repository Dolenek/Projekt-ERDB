using System;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

        public void ShowCaptchaAlert(Window window)
        {
            try
            {
                SystemSounds.Exclamation.Play();
            }
            catch
            {
            }

            try
            {
                _notifyIcon.ShowBalloonTip(
                    5000,
                    "EPIC GUARD detected",
                    "Captcha check detected. Review the bot window now.",
                    Forms.ToolTipIcon.Warning);
            }
            catch
            {
            }

            BringToFront(window);
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
