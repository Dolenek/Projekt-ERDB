using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class DiscordChatClient : IDiscordChatClient
    {
        private readonly WebView2 _web;
        private bool _navigationHandlerAttached;

        public DiscordChatClient(WebView2 web)
        {
            _web = web ?? throw new ArgumentNullException(nameof(web));
        }

        public bool IsReady => _web.CoreWebView2 != null;

        public async Task EnsureInitializedAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicRPGBot.UI", "WebView2");
                Directory.CreateDirectory(dataDir);
                var env = await CoreWebView2Environment.CreateAsync(null, dataDir);
                await _web.EnsureCoreWebView2Async(env);
            }

            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            _web.CoreWebView2.Settings.IsZoomControlEnabled = true;

            if (_navigationHandlerAttached)
            {
                return;
            }

            _web.NavigationCompleted += OnNavigationCompleted;
            _navigationHandlerAttached = true;
        }

        public void Reload()
        {
            if (_web.CoreWebView2 != null)
            {
                _web.CoreWebView2.Reload();
            }
            else
            {
                _web.Reload();
            }
        }

        public Task NavigateToChannelAsync(string url)
        {
            if (_web.CoreWebView2 != null) _web.CoreWebView2.Navigate(url);
            else _web.Source = new Uri(url);
            return Task.CompletedTask;
        }

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
            return UnquoteJson(result);
        }

        public async Task<DiscordMessageSnapshot> GetLatestMessageAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return new DiscordMessageSnapshot(string.Empty, string.Empty);
            }

            const string script = @"
(() => {
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  if (items.length === 0) return JSON.stringify({ id: '', text: '' });
  const last = items[items.length - 1];
  return JSON.stringify({ id: (last.id || ''), text: (last.innerText || '') });
})();
";

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(script);
                var payload = UnquoteJson(json);
                return new DiscordMessageSnapshot(
                    ExtractField(payload, "id"),
                    ExtractField(payload, "text"));
            }
            catch
            {
                return new DiscordMessageSnapshot(string.Empty, string.Empty);
            }
        }

        public async Task<bool> SendMessageAsync(string message)
        {
            if (_web.CoreWebView2 == null)
            {
                return false;
            }

            if (!await FocusComposerAsync())
            {
                return false;
            }

            var text = EscapeMessage(message);

            try
            {
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync(
                    "Input.insertText",
                    $"{{\"text\":\"{text}\"}}");

                const string keyDown = "{\"type\":\"keyDown\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}";
                const string keyUp = "{\"type\":\"keyUp\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}";

                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyDown);
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyUp);
                return true;
            }
            catch
            {
                return await SendWithExecCommandFallbackAsync(text);
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
                var url = UnquoteJson(result);
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
                var payload = UnquoteJson(json);
                var ok = ExtractField(payload, "ok");
                if (string.IsNullOrEmpty(ok) || ok.IndexOf("true", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return null;
                }

                var x = ParseDouble(ExtractField(payload, "x"));
                var y = ParseDouble(ExtractField(payload, "y"));
                var w = ParseDouble(ExtractField(payload, "w"));
                var h = ParseDouble(ExtractField(payload, "h"));
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
                var data = ExtractField(UnquoteJson(response), "data");
                return string.IsNullOrWhiteSpace(data) ? null : Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }
        }

        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                await ClickInterstitialsAsync();
            }
            catch { }
        }

        private async Task ClickInterstitialsAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return;
            }

            const string script = @"
(() => {
  const byText = (txt) => {
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT, null);
    let clicked = false;
    while (walker.nextNode()) {
      const el = walker.currentNode;
      if (!(el instanceof HTMLElement)) continue;
      const t = (el.innerText || '').trim();
      if (!t) continue;
      if (t.includes(txt)) {
        try { el.click(); clicked = true; } catch {}
      }
    }
    return clicked;
  };
  let any = false;
  any = byText('Open Discord in your browser') || any;
  any = byText('Continue to Discord') || any;
  any = byText('Continue in browser') || any;
  return any;
})();
";

            await _web.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async Task<bool> FocusComposerAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return false;
            }

            const string script = @"
(function(){
  let candidates = Array.from(document.querySelectorAll(
    'div[role=""textbox""][data-slate-editor=""true""], ' +
    'div[aria-label^=""Message""][role=""textbox""], ' +
    'div[aria-label*=""Message""][role=""textbox""]'
  ));
  if (candidates.length === 0) {
    candidates = Array.from(document.querySelectorAll('div[role=""textbox""], div[contenteditable=""true""]'));
  }
  const vis = candidates.filter(el => {
    const r = el.getBoundingClientRect();
    const st = window.getComputedStyle(el);
    return r.width > 0 && r.height > 0 && st.visibility !== 'hidden' && st.display !== 'none';
  });
  const list = vis.length ? vis : candidates;
  if (list.length === 0) return false;
  list.sort((a,b) => a.getBoundingClientRect().top - b.getBoundingClientRect().top);
  const editor = list[list.length - 1];
  try { editor.scrollIntoView({block:'center'}); } catch(e) {}
  try { editor.click(); } catch(e) {}
  try { editor.focus(); } catch(e) {}
  try {
    const r = document.createRange();
    r.selectNodeContents(editor);
    r.collapse(false);
    const sel = window.getSelection();
    sel.removeAllRanges();
    sel.addRange(r);
  } catch(e) {}
  return document.activeElement === editor || true;
})();";

            for (var i = 0; i < 10; i++)
            {
                var focused = await _web.CoreWebView2.ExecuteScriptAsync(script);
                if (string.Equals(focused?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                await Task.Delay(500);
            }

            return false;
        }

        private async Task<bool> SendWithExecCommandFallbackAsync(string message)
        {
            try
            {
                var js = $@"
(async () => {{
  const sleep = (ms) => new Promise(r => setTimeout(r, ms));
  let candidates = Array.from(document.querySelectorAll(
    'div[role=""textbox""][data-slate-editor=""true""], ' +
    'div[aria-label^=""Message""][role=""textbox""], ' +
    'div[aria-label*=""Message""][role=""textbox""]'
  ));
  if (candidates.length === 0) {{
    candidates = Array.from(document.querySelectorAll('div[role=""textbox""], div[contenteditable=""true""]'));
  }}
  if (candidates.length === 0) return false;
  candidates.sort((a,b)=>a.getBoundingClientRect().top - b.getBoundingClientRect().top);
  const editor = candidates[candidates.length - 1];
  if (!editor) return false;
  editor.focus();
  try {{
    document.execCommand('insertText', false, ""{message}"");
    await sleep(50);
    const eDown = new KeyboardEvent('keydown', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    const eUp = new KeyboardEvent('keyup', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    editor.dispatchEvent(eDown);
    editor.dispatchEvent(eUp);
    return true;
  }} catch (e) {{ return false; }}
}})();
";

                var result = await _web.CoreWebView2.ExecuteScriptAsync(js);
                return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string EscapeMessage(string message)
        {
            return (message ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        private static double ParseDouble(string raw)
        {
            double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value);
            return value;
        }

        private static string ExtractField(string json, string field)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var key = $"\"{field}\":";
            var startIndex = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                return null;
            }

            startIndex += key.Length;
            while (startIndex < json.Length && json[startIndex] == ' ')
            {
                startIndex++;
            }

            var quoted = startIndex < json.Length && json[startIndex] == '\"';
            if (quoted)
            {
                startIndex++;
            }

            var current = startIndex;
            if (quoted)
            {
                while (current < json.Length)
                {
                    if (json[current] == '\\')
                    {
                        current += 2;
                        continue;
                    }

                    if (json[current] == '\"')
                    {
                        break;
                    }

                    current++;
                }

                return json.Substring(startIndex, Math.Max(0, current - startIndex))
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            while (current < json.Length && json[current] != ',' && json[current] != '}' && !char.IsWhiteSpace(json[current]))
            {
                current++;
            }

            return json.Substring(startIndex, Math.Max(0, current - startIndex)).Trim();
        }

        private static string UnquoteJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            value = value.Trim();
            if (value.Length >= 2 && value[0] == '\"' && value[value.Length - 1] == '\"')
            {
                value = value.Substring(1, value.Length - 2)
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            return value;
        }
    }
}
