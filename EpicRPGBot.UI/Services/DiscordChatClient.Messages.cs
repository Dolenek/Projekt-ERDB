using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<string> GetLastMessageTextAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return null;
            }

            const string script = @"
(() => {
  const items = Array.from(document.querySelectorAll(""li[id^='chat-messages-']""));
  if (items.length === 0) return '';
  const last = items[items.length - 1];
  return last.innerText || '';
})()
";

            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return DiscordScriptParsing.UnquoteJson(result);
        }

        public async Task<DiscordMessageSnapshot> GetLatestMessageAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return new DiscordMessageSnapshot(string.Empty, string.Empty, string.Empty);
            }

            var script = BuildSingleMessageSnapshotScript(@"
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  if (items.length === 0) return JSON.stringify(emptySnapshot());
  return JSON.stringify(mapSnapshot(items[items.length - 1]));
");

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(script);
                return DiscordScriptParsing.ParseSnapshot(DiscordScriptParsing.UnquoteJson(json)) ??
                    new DiscordMessageSnapshot(string.Empty, string.Empty, string.Empty);
            }
            catch
            {
                return new DiscordMessageSnapshot(string.Empty, string.Empty, string.Empty);
            }
        }

        public async Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount)
        {
            if (_web.CoreWebView2 == null)
            {
                return Array.Empty<DiscordMessageSnapshot>();
            }

            var count = Math.Max(1, maxCount);
            var script = BuildRecentMessagesScript(count);

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(script);
                return DiscordScriptParsing.ParseSnapshots(DiscordScriptParsing.UnquoteJson(json));
            }
            catch
            {
                return Array.Empty<DiscordMessageSnapshot>();
            }
        }

        public async Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(outgoingMessageId))
            {
                return null;
            }

            var script = BuildSingleMessageSnapshotScript($@"
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  const outgoingId = '{(outgoingMessageId ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'")}';
  const outgoingIndex = items.findIndex(item => (item.id || '') === outgoingId);
  if (outgoingIndex < 0) {{
    return JSON.stringify(emptySnapshot());
  }}
  const looksLikeEpicReply = (author, text) => {{
    const normalizedAuthor = (author || '').toLowerCase();
    const normalizedText = (text || '').toLowerCase();
    return normalizedAuthor.includes('epic rpg') ||
      normalizedText.includes('epic rpg') ||
      normalizedText.includes('area:') ||
      normalizedText.includes('successfully traded') ||
      normalizedText.includes('you traded') ||
      normalizedText.includes(""you don't have enough items to trade this"") ||
      normalizedText.includes(""don't have enough"") ||
      normalizedText.includes(""you don't have enough items to craft this"") ||
      normalizedText.includes('successfully crafted') ||
      normalizedText.includes('wait at least');
  }};
  let fallback = null;
  for (let j = outgoingIndex + 1; j < items.length; j++) {{
    const snapshot = mapSnapshot(items[j]);
    if (looksLikeEpicReply(snapshot.author, snapshot.text)) {{
      return JSON.stringify(snapshot);
    }}
    if (!fallback) {{
      fallback = snapshot;
    }}
  }}
  return JSON.stringify(fallback || emptySnapshot());
");

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(script);
                var payload = DiscordScriptParsing.ParseSnapshot(DiscordScriptParsing.UnquoteJson(json));
                var id = payload?.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    return null;
                }

                return payload;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            var js = $@"
(() => {{
  const root = document.getElementById('{messageId.Replace("'", "\\'")}');
  if (!root) return '';
  const imgs = Array.from(root.querySelectorAll('img'));
  let best = '';
  for (const im of imgs) {{
    const src = im.getAttribute('src') || im.src || '';
    if (!src) continue;
    const r = im.getBoundingClientRect();
    const styles = window.getComputedStyle(im);
    const visible = r.width > 0 && r.height > 0 && styles.visibility !== 'hidden' && styles.display !== 'none';
    if (visible) {{
      best = src;
      break;
    }}
    if (!best) best = src;
  }}
  return best || '';
}})();";

            try
            {
                var result = await _web.CoreWebView2.ExecuteScriptAsync(js);
                var url = DiscordScriptParsing.UnquoteJson(result);
                return string.IsNullOrWhiteSpace(url) ? null : url;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]> CaptureMessageImagePngAsync(string messageId)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            var js = $@"
(() => {{
  const root = document.getElementById('{messageId.Replace("'", "\\'")}');
  if (!root) return JSON.stringify({{ ok:false }});
  const imgs = Array.from(root.querySelectorAll('img'));
  let el = null;
  for (const im of imgs) {{
    const r = im.getBoundingClientRect();
    const st = window.getComputedStyle(im);
    const vis = r.width > 1 && r.height > 1 && st.visibility !== 'hidden' && st.display !== 'none';
    if (vis) {{ el = im; break; }}
  }}
  if (!el) return JSON.stringify({{ ok:false }});
  try {{ el.scrollIntoView({{block:'center'}}); }} catch {{}}
  const r = el.getBoundingClientRect();
  return JSON.stringify({{
    ok:true,
    x:r.left + window.scrollX,
    y:r.top + window.scrollY,
    w:r.width,
    h:r.height
  }});
}})();";

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(js);
                var payload = DiscordScriptParsing.UnquoteJson(json);
                var ok = DiscordScriptParsing.ExtractField(payload, "ok");
                if (string.IsNullOrEmpty(ok) || ok.IndexOf("true", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return null;
                }

                var x = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "x"));
                var y = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "y"));
                var w = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "w"));
                var h = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "h"));
                if (w < 2 || h < 2)
                {
                    return null;
                }

                var screenshotArgs =
                    $"{{\"format\":\"png\",\"fromSurface\":true,\"captureBeyondViewport\":true," +
                    $"\"clip\":{{\"x\":{Math.Max(0, x).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"y\":{Math.Max(0, y).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"width\":{Math.Max(2, w).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"height\":{Math.Max(2, h).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"scale\":1}}}}";

                var response = await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", screenshotArgs);
                var data = DiscordScriptParsing.ExtractField(DiscordScriptParsing.UnquoteJson(response), "data");
                return string.IsNullOrWhiteSpace(data) ? null : Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildRecentMessagesScript(int count)
        {
            return BuildSingleMessageSnapshotScript($@"
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  const recent = items.slice(-{count});
  return JSON.stringify(recent.map(item => mapSnapshot(item)));
");
        }

        private static string BuildSingleMessageSnapshotScript(string body)
        {
            return $@"
(() => {{
  const emptySnapshot = () => ({{
    id: '',
    text: '',
    author: '',
    renderedText: '',
    buttons: []
  }});
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
  const normalizeImageAlt = (img) => {{
    if (!img) return '';
    const raw = (img.getAttribute('alt') || img.getAttribute('aria-label') || img.getAttribute('title') || '').trim();
    return raw.startsWith(':') && raw.endsWith(':') ? raw : '';
  }};
  const replaceImagesWithAlt = (root) => {{
    Array.from(root.querySelectorAll('img')).forEach(img => {{
      const text = normalizeImageAlt(img);
      img.replaceWith(document.createTextNode(text));
    }});
  }};
  const renderTextWithoutButtons = (item) => {{
    const clone = item.cloneNode(true);
    Array.from(clone.querySelectorAll('button')).forEach(button => button.remove());
    replaceImagesWithAlt(clone);
    return (clone.innerText || '').trim();
  }};
  const getVisibleButtons = (item) => {{
    const visibleButtons = Array.from(item.querySelectorAll('button'))
      .map(button => {{
        const rect = button.getBoundingClientRect();
        const style = window.getComputedStyle(button);
        return {{
          button,
          top: rect.top,
          left: rect.left,
          visible: rect.width > 0 &&
            rect.height > 0 &&
            !button.disabled &&
            style.visibility !== 'hidden' &&
            style.display !== 'none'
        }};
      }})
      .filter(entry => entry.visible)
      .sort((a, b) => a.top - b.top || a.left - b.left);
    const rows = [];
    for (const entry of visibleButtons) {{
      const existing = rows.find(row => Math.abs(row.top - entry.top) <= 12);
      if (existing) {{
        existing.items.push(entry);
        continue;
      }}
      rows.push({{ top: entry.top, items: [entry] }});
    }}
    return rows.flatMap((row, rowIndex) => row.items
      .sort((a, b) => a.left - b.left)
      .map((entry, columnIndex) => {{
        const clone = entry.button.cloneNode(true);
        replaceImagesWithAlt(clone);
        return {{
          label: (clone.innerText || '').trim(),
          rowIndex,
          columnIndex
        }};
      }}));
  }};
  const mapSnapshot = (item) => ({{
    id: item.id || '',
    text: item.innerText || '',
    author: getAuthor(item),
    renderedText: renderTextWithoutButtons(item),
    buttons: getVisibleButtons(item)
  }});
{body}
}})();
";
        }
    }
}
