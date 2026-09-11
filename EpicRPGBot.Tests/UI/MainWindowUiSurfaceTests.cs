using System.Xml.Linq;
using Xunit;

namespace EpicRPGBot.Tests.UI;

public sealed class MainWindowUiSurfaceTests
{
    private const string XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    [Fact]
    public void MainWindowDeclaresCustomChromeAndCompleteControlSurface()
    {
        var document = LoadUiXaml("MainWindow.xaml");
        var root = Assert.IsType<XElement>(document.Root);
        var namedElements = GetNamedElements(document);

        Assert.Equal("None", (string?)root.Attribute("WindowStyle"));
        Assert.Equal("MainWindow_SourceInitialized", (string?)root.Attribute("SourceInitialized"));
        Assert.Equal("/EpicRPGBot.UI;component/Assets/icon.png", (string?)root.Attribute("Icon"));
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "WindowChrome");
        Assert.Equal("300", GetNamedElement(document, "ActivityColumn").Attribute("Width")?.Value);
        Assert.Equal("360", GetNamedElement(document, "ControlCenterColumn").Attribute("Width")?.Value);
        Assert.DoesNotContain(document.Descendants(), element =>
            element.Name.LocalName == "TextBlock" && element.Attribute("Text")?.Value == "Automation");

        var requiredNames = new[]
        {
            "ActivitySearchBox", "ActivityKindFilter", "ConnectionStatusText", "EngineStatusText",
            "StartBtn", "StopBtn", "InitBtn", "RpgCdBtn", "TradeAreaBtn", "WishingTokenBtn",
            "CraftingBtn", "DismantleBtn", "CompleteDungeonBtn", "DuelBtn", "GoChannelBtn",
            "BrowserTabs", "PlayerWeb", "Web", "GuildWeb", "DungeonWeb", "DuelWeb",
            "ActivityCollapseBtn", "ActivityExpandBtn", "ControlCenterCollapseBtn", "ControlCenterExpandBtn"
        };

        Assert.All(requiredNames, name => Assert.Contains(name, namedElements));
    }

    [Fact]
    public void CooldownPanelUsesCompactReadyBadgeStyle()
    {
        var document = LoadUiXaml(Path.Combine("Controls", "CooldownPanelControl.xaml"));
        var readyTrigger = document.Descendants()
            .First(element => element.Name.LocalName == "Trigger" && element.Attribute("Value")?.Value == "READY");
        var countdown = GetNamedElement(document, "DailyCdText");

        Assert.NotNull(readyTrigger);
        Assert.Contains("CooldownValueStyle", countdown.Attribute("Style")?.Value);
        Assert.Equal("READY", countdown.Attribute("Text")?.Value);
        Assert.DoesNotContain(document.Descendants(), element => element.Name.LocalName == "ScrollViewer");
    }

    [Fact]
    public void SettingsWindowUsesSharedResponsiveShell()
    {
        var document = LoadUiXaml(Path.Combine("Settings", "SettingsWindow.xaml"));
        var root = Assert.IsType<XElement>(document.Root);
        var namedElements = GetNamedElements(document);

        Assert.Equal("None", root.Attribute("WindowStyle")?.Value);
        Assert.Equal("SettingsWindow_Loaded", root.Attribute("Loaded")?.Value);
        Assert.Equal("SettingsWindow_SourceInitialized", root.Attribute("SourceInitialized")?.Value);
        Assert.Contains(document.Descendants(), element =>
            element.Name.LocalName == "ResourceDictionary" &&
            element.Attribute("Source")?.Value == "../Themes/SettingsDialogTheme.xaml");
        Assert.Contains(document.Descendants(), element =>
            element.Name.LocalName == "ScrollViewer" &&
            element.Attribute("HorizontalScrollBarVisibility")?.Value == "Disabled");

        var requiredNames = new[]
        {
            "ChannelUrlBox", "DungeonListingChannelUrlBox", "UseAtMeFallback", "AreaBox",
            "HuntCdBox", "AdventureCdBox", "TrainingCdBox", "WorkCdBox", "FarmCdBox",
            "LootboxCdBox", "AscendedCheckBox", "AutoDeleteDungeonChannelCheckBox",
            "WorkCommandsBtn", "GuildRaidBtn", "CardHandBtn", "CloseBtn"
        };
        Assert.All(requiredNames, name => Assert.Contains(name, namedElements));
    }

    [Fact]
    public void MainWindowConstrainsStartupBoundsAndPackagesTheIcon()
    {
        var mainWindowCode = LoadUiText("MainWindow.xaml.cs");
        var repositoryRoot = GetRepositoryRoot();

        Assert.Contains("WindowWorkAreaChrome.ConstrainToWorkArea(this)", mainWindowCode);
        Assert.True(File.Exists(Path.Combine(repositoryRoot, "EpicRPGBot.UI", "Assets", "icon.png")));
        Assert.False(File.Exists(Path.Combine(repositoryRoot, "icon.png")));
    }

    [Fact]
    public void AdvancedSettingsDialogsUseTheSharedResponsiveShell()
    {
        var requiredNames = new Dictionary<string, string[]>
        {
            ["WorkCommandsWindow.xaml"] = ["CloseBtn", "MinimizeBtn", "MaximizeRestoreBtn", "CloseWindowBtn"],
            ["GuildRaidSettingsWindow.xaml"] = ["GuildRaidChannelUrlBox", "GuildRaidTriggerTextBox", "GuildRaidMatchModeBox", "GuildRaidAuthorFilterBox", "CloseBtn"],
            ["CardHandSettingsWindow.xaml"] = ["AutoPlayCheckBox", "LoadDeckButton", "DeckStatusText", "TimeCapsuleWeightBox", "CloseButton"]
        };

        foreach (var dialog in requiredNames)
        {
            var document = LoadUiXaml(Path.Combine("Settings", dialog.Key));
            var root = Assert.IsType<XElement>(document.Root);
            var names = GetNamedElements(document);

            Assert.Equal("None", root.Attribute("WindowStyle")?.Value);
            Assert.Equal("/EpicRPGBot.UI;component/Assets/icon.png", root.Attribute("Icon")?.Value);
            Assert.Contains(document.Descendants(), element => element.Name.LocalName == "WindowChrome");
            Assert.Contains(document.Descendants(), element =>
                element.Name.LocalName == "ResourceDictionary" &&
                element.Attribute("Source")?.Value == "../Themes/SettingsDialogTheme.xaml");
            Assert.Contains(document.Descendants(), element =>
                element.Name.LocalName == "ScrollViewer" &&
                element.Attribute("HorizontalScrollBarVisibility")?.Value == "Disabled");
            Assert.All(dialog.Value, name => Assert.Contains(name, names));
        }
    }

    private static XDocument LoadUiXaml(string relativePath)
    {
        return XDocument.Load(Path.Combine(GetRepositoryRoot(), "EpicRPGBot.UI", relativePath));
    }

    private static string LoadUiText(string relativePath) =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "EpicRPGBot.UI", relativePath));

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static HashSet<string> GetNamedElements(XDocument document)
    {
        return document.Descendants()
            .Select(element => (string?)element.Attribute("Name") ?? (string?)element.Attribute(XName.Get("Name", XamlNamespace)))
            .Where(name => name != null)
            .Cast<string>()
            .ToHashSet(StringComparer.Ordinal);
    }

    private static XElement GetNamedElement(XDocument document, string name)
    {
        return document.Descendants().First(element =>
            (string?)element.Attribute("Name") == name ||
            (string?)element.Attribute(XName.Get("Name", XamlNamespace)) == name);
    }
}
