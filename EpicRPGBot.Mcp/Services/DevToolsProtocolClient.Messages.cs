using EpicRPGBot.Mcp.Models;
using System.Text.Json;

namespace EpicRPGBot.Mcp.Services;

public sealed partial class DevToolsProtocolClient
{
    public async Task<WebViewDebugStateResult> ReadDebugStateAsync()
    {
        var result = await EvaluateAsync(BuildDebugStateScript());
        var payload = JsonSerializer.Deserialize<DebugStatePayload>(result.JsonValue, JsonOptions) ?? new DebugStatePayload();

        return new WebViewDebugStateResult(
            result.TargetUrl,
            result.TargetTitle,
            payload.ReadyState ?? string.Empty,
            payload.TabRole ?? string.Empty,
            payload.BodyPreview ?? string.Empty);
    }

    public async Task<WebViewMessagesResult> ReadRecentMessagesAsync(int limit)
    {
        var actualLimit = Math.Max(1, limit);
        var result = await EvaluateAsync(BuildReadMessagesScript(actualLimit));
        var messages = JsonSerializer.Deserialize<List<WebViewMessageSnapshot>>(result.JsonValue, JsonOptions)
            ?? new List<WebViewMessageSnapshot>();

        return new WebViewMessagesResult(result.TargetUrl, result.TargetTitle, messages);
    }

    public async Task<WebViewWaitResult> WaitForMessageAsync(
        string authorContains,
        string textContains,
        string afterId,
        int timeoutMs,
        int pollIntervalMs = 250)
    {
        if (string.IsNullOrWhiteSpace(authorContains) && string.IsNullOrWhiteSpace(textContains))
        {
            throw new InvalidOperationException("At least one of authorContains or textContains is required.");
        }

        var actualTimeoutMs = Math.Max(200, timeoutMs);
        var actualPollMs = Math.Max(100, pollIntervalMs);
        var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(actualTimeoutMs);
        WebViewMessagesResult latest = new(string.Empty, string.Empty, Array.Empty<WebViewMessageSnapshot>());

        while (DateTime.UtcNow <= deadline)
        {
            latest = await ReadRecentMessagesAsync(30);
            var match = FindMatchingMessage(latest.Messages, authorContains, textContains, afterId);
            if (match != null)
            {
                return new WebViewWaitResult(
                    latest.TargetUrl,
                    latest.TargetTitle,
                    authorContains ?? string.Empty,
                    textContains ?? string.Empty,
                    afterId ?? string.Empty,
                    true,
                    false,
                    match);
            }

            await Task.Delay(actualPollMs);
        }

        return new WebViewWaitResult(
            latest.TargetUrl,
            latest.TargetTitle,
            authorContains ?? string.Empty,
            textContains ?? string.Empty,
            afterId ?? string.Empty,
            false,
            true);
    }

    private static WebViewMessageSnapshot? FindMatchingMessage(
        IReadOnlyList<WebViewMessageSnapshot> messages,
        string authorContains,
        string textContains,
        string afterId)
    {
        if (messages == null || messages.Count == 0)
        {
            return null;
        }

        var startIndex = 0;
        if (!string.IsNullOrWhiteSpace(afterId))
        {
            for (var i = 0; i < messages.Count; i++)
            {
                if (string.Equals(messages[i].Id, afterId, StringComparison.Ordinal))
                {
                    startIndex = i + 1;
                    break;
                }
            }
        }

        for (var i = startIndex; i < messages.Count; i++)
        {
            var message = messages[i];
            if (!string.IsNullOrWhiteSpace(authorContains) &&
                message.Author.IndexOf(authorContains, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(textContains) &&
                message.Text.IndexOf(textContains, StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            return message;
        }

        return null;
    }

    private static string BuildDebugStateScript()
    {
        return @"
(() => {
  const bodyText = (document.body && document.body.innerText) || '';
  return {
    readyState: document.readyState || '',
    tabRole: window.__epicRpGBotTabRole || document.documentElement.getAttribute('data-epicrpg-tab-role') || '',
    bodyPreview: bodyText.slice(0, 500)
  };
})()";
    }

    private static string BuildReadMessagesScript(int limit)
    {
        return $@"
(() => {{
  const getAuthor = (item) => {{
    const selectors = [
      '[id^=""message-username-""]',
      'h3 span[role=""button""]',
      'h3 span[class*=""username""]',
      'span[class*=""username""]'
    ];
    for (const selector of selectors) {{
      const node = item.querySelector(selector);
      const text = (node?.textContent || '').trim();
      if (text) return text;
    }}
    return '';
  }};
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]')).slice(-{limit});
  return items.map(item => ({{
    id: item.id || '',
    author: getAuthor(item),
    text: item.innerText || ''
  }}));
}})()";
    }

    private sealed class DebugStatePayload
    {
        public string? ReadyState { get; set; }

        public string? TabRole { get; set; }

        public string? BodyPreview { get; set; }
    }
}
