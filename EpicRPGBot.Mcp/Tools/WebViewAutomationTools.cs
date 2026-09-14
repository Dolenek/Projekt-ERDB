using System.ComponentModel;
using EpicRPGBot.Mcp.Models;
using EpicRPGBot.Mcp.Services;
using ModelContextProtocol.Server;

namespace EpicRPGBot.Mcp.Tools;

[McpServerToolType]
public sealed class WebViewAutomationTools
{
    private readonly DevToolsProtocolClient _devTools;
    private readonly UiAppSession _session;

    public WebViewAutomationTools(DevToolsProtocolClient devTools, UiAppSession session)
    {
        _devTools = devTools;
        _session = session;
    }

    [McpServerTool, Description("Evaluate JavaScript inside the Discord WebView2 page and return the JSON result.")]
    public Task<WebViewEvalResult> webview_eval(
        [Description("Stable account id shown in the account registry.")] string accountId,
        [Description("A JavaScript expression to evaluate.")] string script)
    {
        return RunEvalAsync(accountId, script);
    }

    [McpServerTool, Description("Capture a screenshot of the Discord WebView2 page through DevTools.")]
    public Task<ImageArtifactResult> webview_capture(
        [Description("Stable account id shown in the account registry.")] string accountId)
    {
        return RunCaptureAsync(accountId);
    }

    [McpServerTool, Description("Read the targeted Discord WebView state, including URL, title, account id, tab role, ready state, and body preview.")]
    public Task<WebViewDebugStateResult> read_webview_debug_state(
        [Description("Stable account id shown in the account registry.")] string accountId)
    {
        return RunDebugStateAsync(accountId);
    }

    [McpServerTool, Description("Read recent Discord WebView messages as parsed {id, author, text} snapshots.")]
    public Task<WebViewMessagesResult> read_recent_webview_messages(
        [Description("Stable account id shown in the account registry.")] string accountId,
        [Description("Maximum number of recent messages to return.")] int limit = 10)
    {
        return RunReadMessagesAsync(accountId, limit);
    }

    [McpServerTool, Description("Wait for a Discord WebView message matching the provided author/text filters.")]
    public Task<WebViewWaitResult> wait_for_webview_message(
        [Description("Stable account id shown in the account registry.")] string accountId,
        [Description("Optional substring expected in the author name.")] string authorContains = "",
        [Description("Optional substring expected in the message text.")] string textContains = "",
        [Description("Optional message id after which the match must appear.")] string afterId = "",
        [Description("Maximum wait time in milliseconds.")] int timeoutMs = 10000)
    {
        return RunWaitForMessageAsync(accountId, authorContains, textContains, afterId, timeoutMs);
    }

    private async Task<WebViewEvalResult> RunEvalAsync(string accountId, string script)
    {
        try
        {
            return (await _devTools.EvaluateAsync(accountId, script)) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new WebViewEvalResult(string.Empty, string.Empty, string.Empty, false, ex.Message, _session.GetStatus());
        }
    }

    private async Task<ImageArtifactResult> RunCaptureAsync(string accountId)
    {
        try
        {
            return (await _devTools.CaptureAsync(accountId)) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new ImageArtifactResult(string.Empty, 0, 0, string.Empty, false, ex.Message, _session.GetStatus());
        }
    }

    private async Task<WebViewDebugStateResult> RunDebugStateAsync(string accountId)
    {
        try
        {
            return (await _devTools.ReadDebugStateAsync(accountId)) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new WebViewDebugStateResult(string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, false, ex.Message, _session.GetStatus());
        }
    }

    private async Task<WebViewMessagesResult> RunReadMessagesAsync(string accountId, int limit)
    {
        try
        {
            return (await _devTools.ReadRecentMessagesAsync(accountId, limit)) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new WebViewMessagesResult(string.Empty, string.Empty, Array.Empty<WebViewMessageSnapshot>(), false, ex.Message, _session.GetStatus());
        }
    }

    private async Task<WebViewWaitResult> RunWaitForMessageAsync(string accountId, string authorContains, string textContains, string afterId, int timeoutMs)
    {
        try
        {
            return (await _devTools.WaitForMessageAsync(accountId, authorContains, textContains, afterId, timeoutMs)) with { Status = _session.GetStatus() };
        }
        catch (Exception ex)
        {
            return new WebViewWaitResult(string.Empty, string.Empty, authorContains ?? string.Empty, textContains ?? string.Empty, afterId ?? string.Empty, false, false, null, false, ex.Message, _session.GetStatus());
        }
    }
}
