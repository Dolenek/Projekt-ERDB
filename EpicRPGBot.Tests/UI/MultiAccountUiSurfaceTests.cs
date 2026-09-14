using System.Xml.Linq;
using Xunit;

namespace EpicRPGBot.Tests.UI;

public sealed class MultiAccountUiSurfaceTests
{
    [Fact]
    public void TitleBarContainsScrollableAccountSwitcherAndAddButton()
    {
        var document = XDocument.Load(Path.Combine(RepositoryRoot(), "EpicRPGBot.UI", "MainWindow.xaml"));
        var accountStrip = NamedElement(document, "AccountStrip");
        var addButton = NamedElement(document, "AddAccountBtn");
        var scrollViewer = Assert.IsType<XElement>(accountStrip.Parent);

        Assert.Equal("ScrollViewer", scrollViewer.Name.LocalName);
        Assert.Equal("Auto", scrollViewer.Attribute("HorizontalScrollBarVisibility")?.Value);
        Assert.Equal("AddAccountBtn_Click", addButton.Attribute("Click")?.Value);
    }

    [Fact]
    public void BrowserSessionsUseProfileAndAccountMarkers()
    {
        var clientSource = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "EpicRPGBot.UI", "Services", "DiscordChatClient.cs"));
        var runtimeSource = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "EpicRPGBot.UI", "Accounts", "AccountRuntime.cs"));

        Assert.Contains("controllerOptions.ProfileName = _profileName", clientSource);
        Assert.Contains("__epicRpGBotAccountId", clientSource);
        Assert.Contains("Definition.BrowserProfileName", runtimeSource);
    }

    [Fact]
    public void McpWebViewTargetingRequiresAccountIdentity()
    {
        var clientSource = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "EpicRPGBot.Mcp", "Services", "DevToolsProtocolClient.cs"));
        var toolSource = File.ReadAllText(Path.Combine(
            RepositoryRoot(), "EpicRPGBot.Mcp", "Tools", "WebViewAutomationTools.cs"));

        Assert.Contains("GetTargetAsync(string accountId)", clientSource);
        Assert.Contains("TryGetAccountIdAsync(candidate)", clientSource);
        Assert.Contains("Could not find an active Bot WebView for account", clientSource);
        Assert.Contains("string accountId", toolSource);
    }

    private static XElement NamedElement(XDocument document, string name)
    {
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        return document.Descendants().Single(element => (string?)element.Attribute(xaml + "Name") == name);
    }

    private static string RepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}
