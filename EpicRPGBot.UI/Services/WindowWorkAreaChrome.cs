using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace EpicRPGBot.UI.Services
{
    internal sealed class WindowWorkAreaChrome
    {
        private const int GetMinMaxInfoMessage = 0x0024;
        private const uint NearestMonitor = 2;
        private readonly Window _window;

        private WindowWorkAreaChrome(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public static void Attach(Window window)
        {
            var hook = new WindowWorkAreaChrome(window);
            var windowHandle = new WindowInteropHelper(window).Handle;
            HwndSource.FromHwnd(windowHandle)?.AddHook(hook.HandleWindowMessage);
        }

        public static void ConstrainToWorkArea(Window window, double margin = 12)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            var workArea = GetMonitorWorkArea(window);
            var safeMargin = Math.Max(0, margin);
            var availableWidth = Math.Max(1, workArea.Width - (safeMargin * 2));
            var availableHeight = Math.Max(1, workArea.Height - (safeMargin * 2));

            window.MinWidth = Math.Min(window.MinWidth, availableWidth);
            window.MinHeight = Math.Min(window.MinHeight, availableHeight);
            window.Width = ResolveLength(window.Width, window.ActualWidth, availableWidth);
            window.Height = ResolveLength(window.Height, window.ActualHeight, availableHeight);

            var minimumLeft = workArea.Left + safeMargin;
            var minimumTop = workArea.Top + safeMargin;
            window.Left = ClampPosition(window.Left, minimumLeft, workArea.Right - window.Width - safeMargin);
            window.Top = ClampPosition(window.Top, minimumTop, workArea.Bottom - window.Height - safeMargin);
        }

        private static Rect GetMonitorWorkArea(Window window)
        {
            var windowHandle = new WindowInteropHelper(window).Handle;
            var monitorHandle = MonitorFromWindow(windowHandle, NearestMonitor);
            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            if (monitorHandle == IntPtr.Zero || !GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return SystemParameters.WorkArea;
            }

            var dpi = VisualTreeHelper.GetDpi(window);
            var width = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left;
            var height = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;
            return new Rect(
                monitorInfo.WorkArea.Left / dpi.DpiScaleX,
                monitorInfo.WorkArea.Top / dpi.DpiScaleY,
                width / dpi.DpiScaleX,
                height / dpi.DpiScaleY);
        }

        private static double ResolveLength(double requested, double actual, double available)
        {
            var preferred = double.IsNaN(requested) || requested <= 0 ? actual : requested;
            return Math.Min(preferred <= 0 ? available : preferred, available);
        }

        private static double ClampPosition(double requested, double minimum, double maximum)
        {
            if (double.IsNaN(requested) || double.IsInfinity(requested))
            {
                return minimum + ((maximum - minimum) / 2);
            }

            return Math.Max(minimum, Math.Min(requested, maximum));
        }

        private IntPtr HandleWindowMessage(
            IntPtr windowHandle,
            int message,
            IntPtr wordParameter,
            IntPtr longParameter,
            ref bool handled)
        {
            if (message == GetMinMaxInfoMessage)
            {
                ApplyMonitorWorkArea(windowHandle, longParameter);
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void ApplyMonitorWorkArea(IntPtr windowHandle, IntPtr minMaxInfoPointer)
        {
            var monitorHandle = MonitorFromWindow(windowHandle, NearestMonitor);
            var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf(typeof(MonitorInfo)) };
            if (monitorHandle == IntPtr.Zero || !GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return;
            }

            var minMaxInfo = (MinMaxInfo)Marshal.PtrToStructure(minMaxInfoPointer, typeof(MinMaxInfo));
            minMaxInfo.MaxPosition.X = monitorInfo.WorkArea.Left - monitorInfo.MonitorArea.Left;
            minMaxInfo.MaxPosition.Y = monitorInfo.WorkArea.Top - monitorInfo.MonitorArea.Top;
            minMaxInfo.MaxSize.X = monitorInfo.WorkArea.Right - monitorInfo.WorkArea.Left;
            minMaxInfo.MaxSize.Y = monitorInfo.WorkArea.Bottom - monitorInfo.WorkArea.Top;
            var dpi = VisualTreeHelper.GetDpi(_window);
            minMaxInfo.MinTrackSize.X = (int)Math.Ceiling(_window.MinWidth * dpi.DpiScaleX);
            minMaxInfo.MinTrackSize.Y = (int)Math.Ceiling(_window.MinHeight * dpi.DpiScaleY);
            Marshal.StructureToPtr(minMaxInfo, minMaxInfoPointer, false);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref MonitorInfo monitorInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRectangle MonitorArea;
            public NativeRectangle WorkArea;
            public uint Flags;
        }
    }
}
