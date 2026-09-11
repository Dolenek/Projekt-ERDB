using System.Xml.Linq;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelUiSurfaceTests
{
    [Fact]
    public void MainWindowDeclaresDuelTabWebViewAndControlWithoutLaunchingDiscord()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var xamlPath = Path.Combine(repositoryRoot, "EpicRPGBot.UI", "MainWindow.xaml");
        var automationPath = Path.Combine(repositoryRoot, "EpicRPGBot.UI", "MainWindow.Automation.cs");
        var document = XDocument.Load(xamlPath);
        var namedElements = document.Descendants()
            .Select(element => (string?)element.Attribute("Name") ?? (string?)element.Attribute(
                XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml")))
            .Where(name => name != null)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("DuelBrowserTab", namedElements);
        Assert.Contains("DuelWeb", namedElements);
        Assert.Contains("DuelBtn", namedElements);

        var automationSource = File.ReadAllText(automationPath);
        Assert.Contains("DuelBrowserTab", automationSource, StringComparison.Ordinal);
        Assert.Contains("DuelDiscordWebView", automationSource, StringComparison.Ordinal);
        Assert.Contains("DuelButton", automationSource, StringComparison.Ordinal);
    }
}
