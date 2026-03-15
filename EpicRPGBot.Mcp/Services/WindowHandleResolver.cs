using System.Diagnostics;

namespace EpicRPGBot.Mcp.Services;

public sealed class WindowHandleResolver
{
    public IntPtr RequireWindowHandle(Process process)
    {
        process.Refresh();
        if (process.MainWindowHandle != IntPtr.Zero)
        {
            return process.MainWindowHandle;
        }

        var enumerated = FindWindowForProcess(process.Id);
        if (enumerated == IntPtr.Zero)
        {
            throw new InvalidOperationException("EpicRPGBot.UI window handle is not available.");
        }

        return enumerated;
    }

    public async Task WaitForWindowAsync(Process process)
    {
        for (var i = 0; i < 120; i++)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException("EpicRPGBot.UI exited before creating a window.");
            }

            process.Refresh();
            if (process.MainWindowHandle != IntPtr.Zero || FindWindowForProcess(process.Id) != IntPtr.Zero)
            {
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("Timed out waiting for EpicRPGBot.UI to open its main window.");
    }

    private static IntPtr FindWindowForProcess(int processId)
    {
        var found = IntPtr.Zero;

        NativeMethods.EnumWindows((handle, _) =>
        {
            if (!NativeMethods.IsWindowVisible(handle))
            {
                return true;
            }

            NativeMethods.GetWindowThreadProcessId(handle, out var ownerPid);
            if (ownerPid != processId)
            {
                return true;
            }

            found = handle;
            return false;
        }, IntPtr.Zero);

        return found;
    }
}
