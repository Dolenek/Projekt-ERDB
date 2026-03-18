using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<bool> ClickMessageButtonAsync(
            string messageId,
            int rowIndex,
            int columnIndex,
            CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null ||
                string.IsNullOrWhiteSpace(messageId) ||
                rowIndex < 0 ||
                columnIndex < 0)
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var escapedMessageId = messageId
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
            var script = $@"
(() => {{
  const root = document.getElementById('{escapedMessageId}');
  if (!root) return false;

  const visibleButtons = Array.from(root.querySelectorAll('button'))
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
    .filter(item => item.visible)
    .sort((a, b) => a.top - b.top || a.left - b.left);

  if (visibleButtons.length === 0) return false;

  const rows = [];
  for (const item of visibleButtons) {{
    const existing = rows.find(row => Math.abs(row.top - item.top) <= 12);
    if (existing) {{
      existing.items.push(item);
      continue;
    }}

    rows.push({{ top: item.top, items: [item] }});
  }}

  if ({rowIndex} >= rows.length) return false;

  const row = rows[{rowIndex}];
  row.items.sort((a, b) => a.left - b.left);
  if ({columnIndex} >= row.items.length) return false;

  const target = row.items[{columnIndex}].button;
  const scrollingElement = document.scrollingElement || document.documentElement;
  const previousScrollLeft = scrollingElement ? scrollingElement.scrollLeft : 0;
  const previousScrollX = window.scrollX || 0;
  try {{ target.scrollIntoView({{ block: 'center', inline: 'nearest' }}); }} catch (e) {{}}
  try {{
    if (scrollingElement) {{
      scrollingElement.scrollLeft = previousScrollLeft;
    }}
    window.scrollTo(previousScrollX, window.scrollY || 0);
  }} catch (e) {{}}
  try {{ target.focus(); }} catch (e) {{}}
  try {{ target.click(); return true; }} catch (e) {{ return false; }}
}})();
";

            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            cancellationToken.ThrowIfCancellationRequested();
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
