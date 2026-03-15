using EpicRPGBot.Mcp.Models;
using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EpicRPGBot.Mcp.Services;

public sealed class DevToolsProtocolClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ArtifactStore _artifacts;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly UiAppSession _session;

    public DevToolsProtocolClient(UiAppSession session, ArtifactStore artifacts)
    {
        _session = session;
        _artifacts = artifacts;
    }

    public async Task<WebViewEvalResult> EvaluateAsync(string script)
    {
        var target = await GetTargetAsync();
        using var response = await SendCommandAsync(
            target.WebSocketDebuggerUrl!,
            "Runtime.evaluate",
            new Dictionary<string, object?>
            {
                ["expression"] = script,
                ["returnByValue"] = true,
                ["awaitPromise"] = true
            });

        ThrowIfError(response.RootElement);
        var result = response.RootElement.GetProperty("result").GetProperty("result");
        var jsonValue = result.TryGetProperty("value", out var value)
            ? value.GetRawText()
            : result.GetRawText();

        return new WebViewEvalResult(target.Url ?? string.Empty, target.Title ?? string.Empty, jsonValue);
    }

    public async Task<ImageArtifactResult> CaptureAsync()
    {
        var target = await GetTargetAsync();
        using var response = await SendCommandAsync(
            target.WebSocketDebuggerUrl!,
            "Page.captureScreenshot",
            new Dictionary<string, object?> { ["format"] = "png", ["fromSurface"] = true });

        ThrowIfError(response.RootElement);
        var base64 = response.RootElement.GetProperty("result").GetProperty("data").GetString();
        if (string.IsNullOrWhiteSpace(base64))
        {
            throw new InvalidOperationException("DevTools did not return screenshot data.");
        }

        var bytes = Convert.FromBase64String(base64);
        var path = _artifacts.SaveBytes("webview", ".png", bytes);
        return new ImageArtifactResult(path, 0, 0, target.Url ?? string.Empty);
    }

    private async Task<DebugTarget> GetTargetAsync()
    {
        var status = _session.GetStatus();
        if (!status.IsRunning || status.DebugPort <= 0)
        {
            throw new InvalidOperationException("EpicRPGBot.UI is not running in automation mode with a DevTools port.");
        }

        var json = await _httpClient.GetStringAsync($"http://127.0.0.1:{status.DebugPort}/json/list");
        var targets = JsonSerializer.Deserialize<List<DebugTarget>>(json, JsonOptions) ?? new List<DebugTarget>();

        var target = targets
            .Where(item => !string.IsNullOrWhiteSpace(item.WebSocketDebuggerUrl))
            .OrderBy(item => item.Type?.Equals("page", StringComparison.OrdinalIgnoreCase) == true ? 0 : 1)
            .ThenBy(item => item.Url?.Contains("discord.com", StringComparison.OrdinalIgnoreCase) == true ? 0 : 1)
            .FirstOrDefault();

        return target ?? throw new InvalidOperationException("Could not find a WebView2 DevTools target.");
    }

    private static void ThrowIfError(JsonElement root)
    {
        if (!root.TryGetProperty("error", out var error))
        {
            return;
        }

        throw new InvalidOperationException(error.GetRawText());
    }

    private static async Task<JsonDocument> SendCommandAsync(string webSocketUrl, string method, IDictionary<string, object?> parameters)
    {
        using var socket = new ClientWebSocket();
        await socket.ConnectAsync(new Uri(webSocketUrl), CancellationToken.None);

        var payload = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["method"] = method,
            ["params"] = parameters
        });

        var bytes = Encoding.UTF8.GetBytes(payload);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

        while (true)
        {
            var message = await ReceiveMessageAsync(socket);
            using var document = JsonDocument.Parse(message);
            if (!document.RootElement.TryGetProperty("id", out var id) || id.GetInt32() != 1)
            {
                continue;
            }

            return JsonDocument.Parse(message);
        }
    }

    private static async Task<string> ReceiveMessageAsync(ClientWebSocket socket)
    {
        var buffer = new byte[8192];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new InvalidOperationException("DevTools closed the WebSocket before replying.");
            }

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private sealed class DebugTarget
    {
        public string? Type { get; set; }

        public string? Title { get; set; }

        public string? Url { get; set; }

        public string? WebSocketDebuggerUrl { get; set; }
    }
}
