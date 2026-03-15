using System;
using System.Collections.Generic;
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
            return DiscordScriptParsing.UnquoteJson(result);
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
                var payload = DiscordScriptParsing.UnquoteJson(json);
                return new DiscordMessageSnapshot(
                    DiscordScriptParsing.ExtractField(payload, "id"),
                    DiscordScriptParsing.ExtractField(payload, "text"));
            }
            catch
            {
                return new DiscordMessageSnapshot(string.Empty, string.Empty);
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
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]'));
  const recent = items.slice(-{count});
  return JSON.stringify(recent.map(item => ({{
    id: item.id || '',
    text: item.innerText || ''
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
        public async Task<bool> SendMessageAsync(string message)
        {
            if (_web.CoreWebView2 == null) return false;
            var latestBeforeSend = await GetLatestMessageAsync();
            if (!await FocusComposerAsync()) return false;
            var text = DiscordScriptParsing.EscapeMessage(message);
            if (await SendWithDevToolsAsync(text) && await WaitForSendCompletionAsync(latestBeforeSend?.Id, message))
            {
                return true;
            }
            await ClearComposerAsync();
            if (!await FocusComposerAsync()) return false;
            if (!await SendWithExecCommandFallbackAsync(text)) return false;
            var completed = await WaitForSendCompletionAsync(latestBeforeSend?.Id, message);
            if (!completed) await ClearComposerAsync();
            return completed;
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
    const selection = window.getSelection();
    if (selection) {{
      const range = document.createRange();
      range.selectNodeContents(editor);
      selection.removeAllRanges();
      selection.addRange(range);
    }}
    try {{ document.execCommand('delete', false, ''); }} catch (e) {{}}
    editor.textContent = '';
    await sleep(50);
    document.execCommand('insertText', false, ""{message}"");
    editor.dispatchEvent(new InputEvent('input', {{ bubbles:true, cancelable:true, inputType:'insertText', data:""{message}"" }}));
    await sleep(100);
    const eDown = new KeyboardEvent('keydown', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    const ePress = new KeyboardEvent('keypress', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    const eUp = new KeyboardEvent('keyup', {{ key:'Enter', code:'Enter', keyCode:13, which:13, bubbles:true, cancelable:true }});
    editor.dispatchEvent(eDown);
    editor.dispatchEvent(ePress);
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
        private async Task<bool> SendWithDevToolsAsync(string message)
        {
            try
            {
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.insertText", $"{{\"text\":\"{message}\"}}");
                const string keyDown = "{\"type\":\"keyDown\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}";
                const string keyUp = "{\"type\":\"keyUp\",\"key\":\"Enter\",\"code\":\"Enter\",\"windowsVirtualKeyCode\":13,\"nativeVirtualKeyCode\":13}";
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyDown);
                await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Input.dispatchKeyEvent", keyUp);
                return true;
            }
            catch { return false; }
        }
        private async Task<bool> WaitForSendCompletionAsync(string previousMessageId, string originalMessage)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                await Task.Delay(150);
                var latest = await GetLatestMessageAsync();
                if (latest != null &&
                    !string.IsNullOrWhiteSpace(latest.Id) &&
                    !string.Equals(latest.Id, previousMessageId, StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(originalMessage) &&
                    latest.Text.IndexOf(originalMessage, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (await IsComposerEmptyAsync()) return true;
            }

            return await IsComposerEmptyAsync();
        }
        private async Task<bool> IsComposerEmptyAsync()
        {
            try
            {
                const string script = @"
(() => {
  let candidates = Array.from(document.querySelectorAll(
    'div[role=""textbox""][data-slate-editor=""true""], ' +
    'div[aria-label^=""Message""][role=""textbox""], ' +
    'div[aria-label*=""Message""][role=""textbox""]'
  ));
  if (candidates.length === 0) {
    candidates = Array.from(document.querySelectorAll('div[role=""textbox""], div[contenteditable=""true""]'));
  }
  if (candidates.length === 0) return true;
  candidates.sort((a,b) => a.getBoundingClientRect().top - b.getBoundingClientRect().top);
  const editor = candidates[candidates.length - 1];
  if (!editor) return true;
  const text = ((editor.innerText || editor.textContent || '').replace(/\u200B/g, '').trim());
  return text.length === 0;
})();
";

                var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
                return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        private async Task ClearComposerAsync()
        {
            if (_web.CoreWebView2 == null) return;

            const string script = @"
(() => {
  const editors = Array.from(document.querySelectorAll('div[role=""textbox""], div[contenteditable=""true""]'));
  if (editors.length === 0) return false;
  editors.sort((a,b) => a.getBoundingClientRect().top - b.getBoundingClientRect().top);
  const editor = editors[editors.length - 1];
  if (!editor) return false;
  editor.textContent = '';
  editor.dispatchEvent(new InputEvent('input', { bubbles:true, cancelable:true, inputType:'deleteContentBackward', data:null }));
  return true;
})();
";

            try { await _web.CoreWebView2.ExecuteScriptAsync(script); } catch { }
        }
    }
}
