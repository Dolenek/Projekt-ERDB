using System.ComponentModel;
using EpicRPGBot.Mcp.Models;
using EpicRPGBot.Mcp.Services;
using ModelContextProtocol.Server;

namespace EpicRPGBot.Mcp.Tools;

[McpServerToolType]
public sealed class WebViewAutomationTools
{
    private readonly DevToolsProtocolClient _devTools;

    public WebViewAutomationTools(DevToolsProtocolClient devTools)
    {
        _devTools = devTools;
    }

    [McpServerTool, Description("Evaluate JavaScript inside the Discord WebView2 page and return the JSON result.")]
    public Task<WebViewEvalResult> webview_eval(
        [Description("A JavaScript expression to evaluate.")] string script)
    {
        return _devTools.EvaluateAsync(script);
    }

    [McpServerTool, Description("Capture a screenshot of the Discord WebView2 page through DevTools.")]
    public Task<ImageArtifactResult> webview_capture()
    {
        return _devTools.CaptureAsync();
    }
}
