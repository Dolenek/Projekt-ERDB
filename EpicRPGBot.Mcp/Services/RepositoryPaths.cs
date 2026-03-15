using System.IO;
using System.Reflection;

namespace EpicRPGBot.Mcp.Services;

public sealed class RepositoryPaths
{
    public RepositoryPaths()
    {
        RootDirectory = FindRootDirectory();
        UiProjectDirectory = Path.Combine(RootDirectory, "EpicRPGBot.UI");
        UiProjectFilePath = Path.Combine(UiProjectDirectory, "EpicRPGBot.UI.csproj");
    }

    public string RootDirectory { get; }

    public string UiProjectDirectory { get; }

    public string UiProjectFilePath { get; }

    private static string FindRootDirectory()
    {
        var current = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "EpicRPGBotCSharp.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate EpicRPGBotCSharp.sln from the MCP server output directory.");
    }
}
