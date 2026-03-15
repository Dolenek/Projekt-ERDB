using System.Diagnostics;
using System.IO;

namespace EpicRPGBot.Mcp.Services;

public sealed class UiBuildArtifactService
{
    private readonly RepositoryPaths _paths;

    public UiBuildArtifactService(RepositoryPaths paths)
    {
        _paths = paths;
    }

    public async Task BuildUiProjectAsync(string configuration)
    {
        var startInfo = new ProcessStartInfo("cmd.exe", $"/c dotnet build EpicRPGBot.UI -c {configuration}")
        {
            WorkingDirectory = _paths.RootDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet build.");
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"dotnet build EpicRPGBot.UI failed.{Environment.NewLine}{stdout}{Environment.NewLine}{stderr}");
        }
    }

    public bool TryResolveExecutablePath(string configuration, out string executablePath)
    {
        var binRoot = Path.Combine(_paths.UiProjectDirectory, "bin", configuration);
        if (!Directory.Exists(binRoot))
        {
            executablePath = string.Empty;
            return false;
        }

        var candidates = Directory.GetFiles(binRoot, "EpicRPGBot.UI.exe", SearchOption.AllDirectories)
            .OrderBy(path => path.Contains("win-x64", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(path => path.Length)
            .ToArray();

        executablePath = candidates.FirstOrDefault() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(executablePath);
    }
}
