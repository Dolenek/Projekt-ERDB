using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<bool> AddReactionAsync(
            string messageId,
            string emojiName,
            CancellationToken cancellationToken = default)
        {
            if (!CanInteractWithMessage(messageId, emojiName))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var existingState = await ReadReactionStateAsync(messageId, emojiName);
            if (existingState == "mine")
            {
                return true;
            }

            if (existingState == "other")
            {
                return false;
            }

            if (!await OpenReactionPickerAsync(messageId))
            {
                return false;
            }

            await Task.Delay(200, cancellationToken);
            var selected = false;
            for (var attempt = 0; attempt < 8 && !selected; attempt++)
            {
                selected = await SearchAndChooseReactionAsync(emojiName);
                if (!selected)
                {
                    await Task.Delay(150, cancellationToken);
                }
            }

            if (!selected)
            {
                return false;
            }

            await Task.Delay(300, cancellationToken);
            return string.Equals(
                await ReadReactionStateAsync(messageId, emojiName),
                "mine",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<bool> RemoveOwnReactionAsync(
            string messageId,
            string emojiName,
            CancellationToken cancellationToken = default)
        {
            if (!CanInteractWithMessage(messageId, emojiName))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var result = await ExecuteReactionButtonScriptAsync(messageId, emojiName, true);
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanInteractWithMessage(string messageId, string emojiName)
        {
            return _web.CoreWebView2 != null &&
                   !string.IsNullOrWhiteSpace(messageId) &&
                   !string.IsNullOrWhiteSpace(emojiName);
        }

        private async Task<string> ReadReactionStateAsync(string messageId, string emojiName)
        {
            var escapedId = EscapeJavaScriptString(messageId);
            var escapedEmoji = EscapeJavaScriptString(emojiName);
            var script = $@"
(() => {{
  const root = document.getElementById('{escapedId}');
  if (!root) return 'none';
  const expected = '{escapedEmoji}'.toLowerCase().replace(/^:|:$/g, '');
  const button = Array.from(root.querySelectorAll('button')).find(candidate => {{
    const alt = (candidate.querySelector('img')?.getAttribute('alt') || '').toLowerCase().replace(/^:|:$/g, '');
    const label = (candidate.getAttribute('aria-label') || '').toLowerCase();
    return alt === expected || label.includes(expected) || (expected === 'white_check_mark' && alt === '✅');
  }});
  if (!button) return 'none';
  return button.getAttribute('aria-pressed') === 'true' ? 'mine' : 'other';
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return DiscordScriptParsing.UnquoteJson(result);
        }

        private Task<string> ExecuteReactionButtonScriptAsync(string messageId, string emojiName, bool requireMine)
        {
            var escapedId = EscapeJavaScriptString(messageId);
            var escapedEmoji = EscapeJavaScriptString(emojiName);
            var mineCheck = requireMine ? "button.getAttribute('aria-pressed') === 'true'" : "true";
            var script = $@"
(() => {{
  const root = document.getElementById('{escapedId}');
  if (!root) return false;
  const expected = '{escapedEmoji}'.toLowerCase().replace(/^:|:$/g, '');
  const matches = button => {{
    const alt = (button.querySelector('img')?.getAttribute('alt') || '').toLowerCase().replace(/^:|:$/g, '');
    const label = (button.getAttribute('aria-label') || '').toLowerCase();
    return alt === expected || label.includes(expected) || (expected === 'white_check_mark' && alt === '✅');
  }};
  const button = Array.from(root.querySelectorAll('button')).find(candidate => matches(candidate) && {mineCheck});
  if (!button) return false;
  button.click();
  return true;
}})();";
            return _web.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async Task<bool> OpenReactionPickerAsync(string messageId)
        {
            var escapedId = EscapeJavaScriptString(messageId);
            var script = $@"
(() => {{
  const root = document.getElementById('{escapedId}');
  if (!root) return false;
  ['mouseover', 'mouseenter', 'mousemove'].forEach(type =>
    root.dispatchEvent(new MouseEvent(type, {{ bubbles: true, view: window }})));
  const labels = ['add reaction', 'přidat reakci'];
  const buttons = Array.from(root.querySelectorAll('button'));
  const button = buttons.find(candidate => {{
    const label = `${{candidate.getAttribute('aria-label') || ''}} ${{candidate.getAttribute('title') || ''}}`.toLowerCase();
    return labels.some(value => label.includes(value));
  }});
  if (!button) return false;
  button.click();
  return true;
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> SearchAndChooseReactionAsync(string emojiName)
        {
            var escapedEmoji = EscapeJavaScriptString(emojiName);
            var script = $@"
(() => {{
  const expected = '{escapedEmoji}'.replace(/^:|:$/g, '');
  const inputs = Array.from(document.querySelectorAll('input'));
  const search = inputs.find(input => {{
    const label = `${{input.placeholder || ''}} ${{input.getAttribute('aria-label') || ''}}`.toLowerCase();
    return label.includes('emoji') || label.includes('reaction') || label.includes('emodži');
  }});
  if (search) {{
    const setter = Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value')?.set;
    setter?.call(search, expected);
    search.dispatchEvent(new Event('input', {{ bubbles: true }}));
  }}
  const normalize = value => (value || '').toLowerCase().replace(/^:|:$/g, '').trim();
  const candidates = Array.from(document.querySelectorAll('[role=""dialog""] button, [class*=""emojiPicker""] button'));
  const target = candidates.find(button => {{
    const alt = normalize(button.querySelector('img')?.getAttribute('alt'));
    const label = normalize(button.getAttribute('aria-label') || button.getAttribute('data-name'));
    return alt === expected.toLowerCase() || label.includes(expected.toLowerCase()) ||
      (expected.toLowerCase() === 'white_check_mark' && alt === '✅');
  }});
  if (!target) return false;
  target.click();
  return true;
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

    }
}
