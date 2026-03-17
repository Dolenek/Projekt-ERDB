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

            const string script = @"
(() => {
  const getAuthor = (item) => {
    const selectors = [
      '[id^=""message-username-""]',
      'h3 span[role=""button""]',
      'h3 span[class*=""username""]',
      'span[class*=""username""]'
    ];
    for (const selector of selectors) {
      const node = item.querySelector(selector);
      const text = (node?.textContent || '').trim();
      if (text) return text;
    }
    return '';
  };
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  if (items.length === 0) return JSON.stringify({ id: '', text: '', author: '' });
  const last = items[items.length - 1];
  return JSON.stringify({
    id: (last.id || ''),
    text: (last.innerText || ''),
    author: getAuthor(last)
  });
})();
";

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(script);
                var payload = DiscordScriptParsing.UnquoteJson(json);
                return new DiscordMessageSnapshot(
                    DiscordScriptParsing.ExtractField(payload, "id"),
                    DiscordScriptParsing.ExtractField(payload, "text"),
                    DiscordScriptParsing.ExtractField(payload, "author"));
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
            var script = $@"
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
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  const recent = items.slice(-{count});
  return JSON.stringify(recent.map(item => ({{
    id: item.id || '',
    text: item.innerText || '',
    author: getAuthor(item)
  }})));
}})();
";

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

        public async Task<bool> HasEpicReplyForCommandAfterMessageAsync(string anchorMessageId, string command)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(command))
            {
                return false;
            }

            var script = $@"
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
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  const anchorId = '{(anchorMessageId ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'")}';
  const target = '{command.Replace("\\", "\\\\").Replace("'", "\\'")}'.toLowerCase();
  let startIndex = 0;
  if (anchorId) {{
    const foundIndex = items.findIndex(item => (item.id || '') === anchorId);
    if (foundIndex >= 0) startIndex = foundIndex + 1;
  }}
  let outgoingIndex = -1;
  for (let i = items.length - 1; i >= startIndex; i--) {{
    const author = getAuthor(items[i]);
    const text = items[i].innerText || '';
    if (author.includes('EPIC RPG') || text.includes('EPIC RPG')) continue;
    if (text.toLowerCase().includes(target)) {{
      outgoingIndex = i;
      break;
    }}
  }}
  if (outgoingIndex < 0) return false;
  for (let j = outgoingIndex + 1; j < items.length; j++) {{
      const author = getAuthor(items[j]);
      const text = items[j].innerText || '';
      if (author.includes('EPIC RPG') || text.includes('EPIC RPG')) {{
        return true;
      }}
  }}
  return false;
}})();
";

            try
            {
                var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
                return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
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
    }
}
