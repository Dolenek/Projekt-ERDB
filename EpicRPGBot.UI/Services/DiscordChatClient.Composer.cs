using System;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<bool> SendMessageAsync(string message)
        {
            if (_web.CoreWebView2 == null)
            {
                return false;
            }

            var latestBeforeSend = await GetLatestMessageAsync();
            if (!await FocusComposerAsync())
            {
                return false;
            }

            var text = DiscordScriptParsing.EscapeMessage(message);
            if (await SendWithDevToolsAsync(text) && await WaitForSendCompletionAsync(latestBeforeSend?.Id, message))
            {
                return true;
            }

            await ClearComposerAsync();
            if (!await FocusComposerAsync())
            {
                return false;
            }

            if (!await SendWithExecCommandFallbackAsync(text))
            {
                return false;
            }

            var completed = await WaitForSendCompletionAsync(latestBeforeSend?.Id, message);
            if (!completed)
            {
                await ClearComposerAsync();
            }

            return completed;
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
            catch
            {
                return false;
            }
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

                if (await IsComposerEmptyAsync())
                {
                    return true;
                }
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
            catch
            {
                return false;
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
