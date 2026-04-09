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
  const looksLikeTrainingPrompt = (value) => {{
    const normalized = (value || '').toLowerCase();
    return normalized.includes('is training in') && normalized.includes('15 seconds');
  }};
  const looksLikeEpicReply = (snapshot) => {{
    const author = snapshot.author || '';
    const text = snapshot.text || '';
    const renderedText = snapshot.renderedText || '';
    const normalizedAuthor = (author || '').toLowerCase();
    const normalizedText = (text || '').toLowerCase();
    const normalizedRenderedText = (renderedText || '').toLowerCase();
    return normalizedAuthor.includes('epic rpg') ||
      normalizedText.includes('epic rpg') ||
      normalizedRenderedText.includes('epic rpg') ||
      looksLikeTrainingPrompt(renderedText) ||
      looksLikeTrainingPrompt(text) ||
      normalizedText.includes('area:') ||
      normalizedText.includes('successfully traded') ||
      normalizedText.includes('you traded') ||
      normalizedText.includes(""you don't have enough items to trade this"") ||
      normalizedText.includes(""don't have enough"") ||
      normalizedText.includes('middle of a command') ||
      normalizedText.includes(""you don't have enough items to craft this"") ||
      normalizedText.includes('successfully crafted') ||
      normalizedText.includes('wait at least');
  }};
  let fallback = null;
  for (let j = outgoingIndex + 1; j < items.length; j++) {{
    const snapshot = mapSnapshot(items[j]);
    if (looksLikeEpicReply(snapshot)) {{
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
    buttons: [],
    mentions: []
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
  const getMentions = (item) => {{
    const seen = new Set();
    const candidates = Array.from(item.querySelectorAll(
      '[data-user-id], span[class*=""mention""], a[href*=""/users/""]'
    ));
    const mentions = [];
    for (const node of candidates) {{
      const element = node instanceof HTMLElement ? node : node.parentElement;
      if (!element) continue;
      const label = (element.innerText || element.textContent || '').trim();
      if (!label || label.indexOf('@') < 0) continue;
      const owner = element.closest('[data-user-id]');
      let userId = (element.getAttribute('data-user-id') || owner?.getAttribute('data-user-id') || '').trim();
      if (!userId) {{
        const href = (element.getAttribute('href') || '').trim();
        const match = href.match(/\/users\/(\d+)/i);
        if (match) {{
          userId = match[1];
        }}
      }}
      if (!userId) continue;
      const key = `${{userId}}|${{label}}`;
      if (seen.has(key)) continue;
      seen.add(key);
      mentions.push({{ label, userId }});
    }}
    return mentions;
  }};
  const mapSnapshot = (item) => ({{
    id: item.id || '',
    text: item.innerText || '',
    author: getAuthor(item),
    renderedText: renderTextWithoutButtons(item),
    buttons: getVisibleButtons(item),
    mentions: getMentions(item)
  }});
{body}
}})();
";
        }
    }
}
