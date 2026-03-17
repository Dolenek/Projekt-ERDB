using System.ComponentModel;
using EpicRPGBot.Mcp.Models;
using EpicRPGBot.Mcp.Services;
using ModelContextProtocol.Server;

namespace EpicRPGBot.Mcp.Tools;

[McpServerToolType]
public sealed class AppAutomationTools
{
    private readonly UiAppSession _session;
    private readonly UiAutomationFacade _ui;

    public AppAutomationTools(UiAppSession session, UiAutomationFacade ui)
    {
        _session = session;
        _ui = ui;
    }

    [McpServerTool, Description("Build and launch EpicRPGBot.UI in automation mode.")]
    public Task<AppStatusResult> launch_app(
        [Description("Build configuration, usually Debug or Release.")] string configuration = "Debug",
        [Description("Rebuild the UI project before launch.")] bool rebuild = true,
        [Description("Optional fixed DevTools port.")] int? debugPort = null)
    {
        return _session.LaunchAsync(configuration, rebuild, debugPort);
    }

    [McpServerTool, Description("Close the EpicRPGBot.UI app launched by this MCP server.")]
    public Task<CloseAppResult> close_app()
    {
        return _session.CloseAsync();
    }

    [McpServerTool, Description("Read the current automation app status without launching or focusing it.")]
    public AppStatusResult get_app_status()
    {
        return _session.GetStatus();
    }

    [McpServerTool, Description("Bring the EpicRPGBot.UI window to the foreground.")]
    public AppStatusResult bring_to_front()
    {
        return _ui.BringToFront();
    }

    [McpServerTool, Description("Capture a screenshot of the EpicRPGBot.UI main window.")]
    public ImageArtifactResult capture_window()
    {
        try
        {
            return _ui.CaptureWindow() with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new ImageArtifactResult(string.Empty, 0, 0, "window", false, ex.Message, _session.GetStatus());
        }
    }

    [McpServerTool, Description("List automation-discoverable controls from the EpicRPGBot.UI window.")]
    public IReadOnlyList<ControlSnapshot> list_controls()
    {
        return _ui.ListControls();
    }

    [McpServerTool, Description("Click a WPF control by AutomationId.")]
    public ControlActionResult click_control(
        [Description("The control AutomationId, for example StartButton or GoChannelButton.")] string automationId)
    {
        try
        {
            return _ui.ClickControl(automationId) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new ControlActionResult(automationId, "click", string.Empty, false, ex.Message, _session.GetStatus());
        }
    }

    [McpServerTool, Description("Set the text or toggle state of a WPF control by AutomationId.")]
    public ControlActionResult set_text(
        [Description("The control AutomationId.")] string automationId,
        [Description("The value to write. For checkboxes use true or false.")] string value)
    {
        try
        {
            return _ui.SetText(automationId, value) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new ControlActionResult(automationId, "set_text", value ?? string.Empty, false, ex.Message, _session.GetStatus());
        }
    }

    [McpServerTool, Description("Read the current text or toggle state of a WPF control by AutomationId.")]
    public TextReadResult get_text(
        [Description("The control AutomationId.")] string automationId)
    {
        try
        {
            return _ui.GetText(automationId) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new TextReadResult(automationId, string.Empty, false, ex.Message, _session.GetStatus());
        }
    }

    [McpServerTool, Description("Read the Console tab list items from the UI.")]
    public Task<ListReadResult> read_console(
        [Description("Maximum number of lines to return from the end of the list.")] int limit = 40)
    {
        return _ui.ReadSidebarListAsync("ConsoleTab", "ConsoleList", limit);
    }

    [McpServerTool, Description("Read the Last messages tab list items from the UI.")]
    public Task<ListReadResult> read_last_messages(
        [Description("Maximum number of lines to return from the end of the list.")] int limit = 10)
    {
        return _ui.ReadSidebarListAsync("LastMessagesTab", "LastMessagesList", limit);
    }

    [McpServerTool, Description("Wait for a WPF control text value to contain a substring.")]
    public Task<WaitForTextResult> wait_for_control_text(
        [Description("The control AutomationId.")] string automationId,
        [Description("Substring that must appear in the control text.")] string textContains,
        [Description("Maximum wait time in milliseconds.")] int timeoutMs = 5000)
    {
        return _ui.WaitForControlTextAsync(automationId, textContains, timeoutMs);
    }
}
