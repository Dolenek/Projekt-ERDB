using EpicRPGBot.Mcp.Models;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace EpicRPGBot.Mcp.Services;

public sealed class UiAppSession
{
    private readonly UiBuildArtifactService _artifacts;
    private readonly WindowHandleResolver _windowResolver;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Process? _process;
    private string _sessionId = string.Empty;
    private string _windowTitle = string.Empty;
    private string _executablePath = string.Empty;
    private int _debugPort;

    public UiAppSession(UiBuildArtifactService artifacts, WindowHandleResolver windowResolver)
    {
        _artifacts = artifacts;
        _windowResolver = windowResolver;
    }

    public int DebugPort => _debugPort;

    public async Task<AppStatusResult> LaunchAsync(string configuration, bool rebuild, int? debugPort)
    {
        await _gate.WaitAsync();
        try
        {
            if (IsRunning())
            {
                return BuildStatus();
            }

            if (rebuild || !_artifacts.TryResolveExecutablePath(configuration, out _))
            {
                await _artifacts.BuildUiProjectAsync(configuration);
            }

            if (!_artifacts.TryResolveExecutablePath(configuration, out var executablePath))
            {
                throw new FileNotFoundException("Could not locate EpicRPGBot.UI.exe after build.", executablePath);
            }

            _debugPort = debugPort.GetValueOrDefault(AllocateDebugPort());
            _sessionId = Guid.NewGuid().ToString("N");
            _executablePath = executablePath;

            var startInfo = new ProcessStartInfo(executablePath, BuildArguments())
            {
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
                UseShellExecute = false
            };

            _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start EpicRPGBot.UI.");
            await _windowResolver.WaitForWindowAsync(_process);
            _windowTitle = _process.MainWindowTitle ?? string.Empty;
            return BuildStatus();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CloseAppResult> CloseAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (!IsRunning())
            {
                Reset();
                return new CloseAppResult(true, 0);
            }

            if (_process!.CloseMainWindow() && _process.WaitForExit(4000))
            {
                var exitCode = _process.ExitCode;
                Reset();
                return new CloseAppResult(true, exitCode);
            }

            _process.Kill(true);
            _process.WaitForExit(4000);
            var forcedExitCode = _process.ExitCode;
            Reset();
            return new CloseAppResult(true, forcedExitCode);
        }
        finally
        {
            _gate.Release();
        }
    }

    public IntPtr RequireWindowHandle()
    {
        if (!IsRunning())
        {
            throw new InvalidOperationException("EpicRPGBot.UI is not running.");
        }

        var handle = _windowResolver.RequireWindowHandle(_process!);
        _windowTitle = _process!.MainWindowTitle ?? _windowTitle;
        return handle;
    }

    public AppStatusResult GetStatus()
    {
        return BuildStatus();
    }

    public void BringToFront()
    {
        var handle = RequireWindowHandle();
        NativeMethods.ShowWindow(handle, NativeMethods.SwRestore);
        NativeMethods.SetForegroundWindow(handle);
    }

    private bool IsRunning()
    {
        return _process != null && !_process.HasExited;
    }

    private AppStatusResult BuildStatus()
    {
        var processId = IsRunning() ? _process!.Id : 0;
        if (IsRunning())
        {
            _process!.Refresh();
            _windowTitle = _process.MainWindowTitle ?? _windowTitle;
        }

        return new AppStatusResult(IsRunning(), processId, _debugPort, _sessionId, _windowTitle, _executablePath);
    }

    private string BuildArguments()
    {
        return $"--automation --automation-debug-port {_debugPort} --automation-session {_sessionId}";
    }

    private static int AllocateDebugPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private void Reset()
    {
        _process = null;
        _sessionId = string.Empty;
        _windowTitle = string.Empty;
        _executablePath = string.Empty;
        _debugPort = 0;
    }
}
