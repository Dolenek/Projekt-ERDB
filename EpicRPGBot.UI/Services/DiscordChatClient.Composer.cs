using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        private const int OutgoingMessagePollAttempts = 20;
        private const int OutgoingMessagePollDelayMs = 200;
        private const int OutgoingMessageScanCount = 12;

        public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
        {
            return await SendMessageAndWaitForOutgoingAsync(message, cancellationToken) != null;
        }

        public async Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(string message, CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null)
            {
                return null;
            }

            var latestBeforeSend = await GetLatestMessageAsync();
            if (!await FocusComposerAsync(cancellationToken))
            {
                return null;
            }

            var text = DiscordScriptParsing.EscapeMessage(message);
            if (await SendWithDevToolsAsync(text))
            {
                var registered = await WaitForOutgoingMessageAsync(latestBeforeSend?.Id, message, cancellationToken);
                if (registered != null)
                {
                    return registered;
                }
            }

            await ClearComposerAsync();
            if (!await FocusComposerAsync(cancellationToken))
            {
                return null;
            }

            if (!await SendWithExecCommandFallbackAsync(text))
            {
                return null;
            }

            var fallbackRegistered = await WaitForOutgoingMessageAsync(latestBeforeSend?.Id, message, cancellationToken);
            if (fallbackRegistered == null)
            {
                await ClearComposerAsync();
            }

            return fallbackRegistered;
        }

        private async Task<bool> FocusComposerAsync(CancellationToken cancellationToken)
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
                cancellationToken.ThrowIfCancellationRequested();
                var focused = await _web.CoreWebView2.ExecuteScriptAsync(script);
                if (string.Equals(focused?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                await Task.Delay(500, cancellationToken);
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
            catch
            {
                return false;
            }
        }

        private async Task<DiscordMessageSnapshot> WaitForOutgoingMessageAsync(
            string previousMessageId,
            string originalMessage,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < OutgoingMessagePollAttempts; attempt++)
            {
                await Task.Delay(OutgoingMessagePollDelayMs, cancellationToken);
                var registered = await FindOutgoingMessageAfterAsync(previousMessageId, originalMessage);
                if (registered != null)
                {
                    return registered;
                }
            }

            return null;
        }

        private async Task<DiscordMessageSnapshot> FindOutgoingMessageAfterAsync(
            string previousMessageId,
            string originalMessage)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(originalMessage))
            {
                return null;
            }

            var escapedPreviousId = (previousMessageId ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
            var escapedMessage = originalMessage
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
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
  const previousId = '{escapedPreviousId}';
  const target = '{escapedMessage}'.toLowerCase();
  const items = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]')).slice(-{OutgoingMessageScanCount});
  let startIndex = 0;
  if (previousId) {{
    const foundIndex = items.findIndex(item => (item.id || '') === previousId);
    if (foundIndex >= 0) startIndex = foundIndex + 1;
  }}
  for (let i = items.length - 1; i >= startIndex; i--) {{
    const item = items[i];
    const id = item.id || '';
    if (!id || id === previousId) continue;
    const author = getAuthor(item);
    const text = item.innerText || '';
    if (author.includes('EPIC RPG') || text.includes('EPIC RPG')) continue;
    if (text.toLowerCase().includes(target)) {{
      return JSON.stringify({{ id, text, author }});
    }}
  }}
  return JSON.stringify({{ id: '', text: '', author: '' }});
}})();
";

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(script);
                var payload = DiscordScriptParsing.UnquoteJson(json);
                var id = DiscordScriptParsing.ExtractField(payload, "id");
                if (string.IsNullOrWhiteSpace(id))
                {
                    return null;
                }

                return new DiscordMessageSnapshot(
                    id,
                    DiscordScriptParsing.ExtractField(payload, "text"),
                    DiscordScriptParsing.ExtractField(payload, "author"));
            }
            catch
            {
                return null;
            }
        }

        private async Task ClearComposerAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return;
            }

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

            try
            {
                await _web.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch
            {
            }
        }
    }
}
