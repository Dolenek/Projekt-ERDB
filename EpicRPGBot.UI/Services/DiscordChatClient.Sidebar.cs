using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<bool> OpenDirectMessageAsync(
            string conversationName,
            CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(conversationName))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var escapedName = conversationName
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
            var script = $@"
(() => {{
  const normalize = (value) => (value || '')
    .toLowerCase()
    .replace(/\s+/g, ' ')
    .trim();
  const target = normalize('{escapedName}');
  const getLabel = (element) => {{
    if (!(element instanceof HTMLElement)) return '';
    const parts = [
      element.innerText,
      element.textContent,
      element.getAttribute('aria-label'),
      element.getAttribute('title'),
      element.getAttribute('data-dnd-name')
    ];
    return normalize(parts.filter(Boolean).join(' '));
  }};
  const isVisible = (element) => {{
    if (!(element instanceof HTMLElement)) return false;
    const rect = element.getBoundingClientRect();
    const style = window.getComputedStyle(element);
    return rect.width > 0 &&
      rect.height > 0 &&
      rect.left < Math.max(window.innerWidth * 0.65, 720) &&
      style.visibility !== 'hidden' &&
      style.display !== 'none';
  }};
  const selector = [
    '[data-list-item-id^=""private-channels-""]',
    'a[href*=""/channels/@me/""]',
    'div[role=""link""]',
    'div[role=""treeitem""]'
  ].join(', ');
  const rawCandidates = Array.from(document.querySelectorAll(selector));
  const candidates = rawCandidates
    .map(element => {{
      const clickable = element.closest('a[href*=""/channels/@me/""], div[role=""link""], div[role=""treeitem""]') || element;
      const rect = clickable.getBoundingClientRect();
      const label = getLabel(element) || getLabel(clickable);
      return {{
        clickable,
        label,
        left: rect.left,
        top: rect.top,
        exact: label === target
      }};
    }})
    .filter(item => item.label.includes(target) && isVisible(item.clickable))
    .sort((a, b) => (a.exact === b.exact)
      ? (a.left - b.left || a.top - b.top)
      : (a.exact ? -1 : 1));
  if (candidates.length === 0) return false;
  const targetEntry = candidates[0].clickable;
  try {{ targetEntry.scrollIntoView({{ block: 'center', inline: 'nearest' }}); }} catch (e) {{}}
  try {{ targetEntry.focus(); }} catch (e) {{}}
  try {{ targetEntry.click(); return true; }} catch (e) {{ return false; }}
}})();
";

            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            cancellationToken.ThrowIfCancellationRequested();
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
