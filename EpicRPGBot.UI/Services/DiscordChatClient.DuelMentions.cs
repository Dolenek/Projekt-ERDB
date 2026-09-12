using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        private const string UnreadStoreLookupScript = @"
  const findUnreadStore = () => {
    const cached = window.__epicRpgDuelUnreadStore;
    if (cached && typeof cached.getMentionCount === 'function') return cached;
    const now = Date.now();
    if (now - Number(window.__epicRpgDuelUnreadProbeUtc || 0) < 5000) return null;
    window.__epicRpgDuelUnreadProbeUtc = now;
    let webpackRequire = window.__epicRpgDuelWebpackRequire;
    try {
      if (!webpackRequire && Array.isArray(window.webpackChunkdiscord_app)) {
        const chunkId = `epic-rpg-duel-${Date.now()}-${Math.random()}`;
        window.webpackChunkdiscord_app.push([[chunkId], {}, runtime => { webpackRequire = runtime; }]);
        window.__epicRpgDuelWebpackRequire = webpackRequire;
      }
      for (const module of Object.values(webpackRequire?.c || {})) {
        const exported = module?.exports;
        const candidates = [exported, exported?.default, ...Object.values(exported || {})];
        const store = candidates.find(candidate =>
          candidate && typeof candidate.getMentionCount === 'function');
        if (store) { window.__epicRpgDuelUnreadStore = store; return store; }
      }
    } catch { }
    return null;
  };
  const unreadStore = findUnreadStore();";

        private const string DomBadgeLookupScript = @"
    const exact = document.querySelector(`[data-list-item-id=""channels___${channelId}""]`);
    const node = exact || Array.from(document.querySelectorAll('[data-list-item-id], [id]'))
      .find(candidate => `${candidate.getAttribute('data-list-item-id') || ''} ${candidate.id || ''}`
        .includes(channelId));
    if (!node) { counts[channelId] = 0; continue; }
    const root = node.closest('li, [role=""treeitem""]') || node.parentElement || node;
    const candidates = [root, ...root.querySelectorAll(
      '[class*=""numberBadge""], [class*=""mention""], [aria-label*=""mention"" i], [aria-label*=""zmín"" i]')];
    let count = 0;
    for (const candidate of candidates) {
      const classText = `${candidate.className || ''}`;
      const ariaText = candidate.getAttribute?.('aria-label') || '';
      const titleText = candidate.getAttribute?.('title') || '';
      const innerText = candidate.innerText || '';
      if (!/mention|zmín/i.test(`${ariaText} ${titleText}`) && !/numberBadge/i.test(classText)) continue;
      const match = `${ariaText} ${titleText}`.match(/(\d+)[^\d]{0,20}(?:mention|zmín)/i) ||
        innerText.match(/^\s*(\d+)\s*$/);
      count = Math.max(count, match ? Number(match[1]) : 1);
    }";

        public async Task<IReadOnlyDictionary<string, int>> GetChannelMentionCountsAsync(
            IReadOnlyList<string> channelIds,
            CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null || channelIds == null || channelIds.Count == 0)
            {
                return new Dictionary<string, int>();
            }

            cancellationToken.ThrowIfCancellationRequested();
            var ids = string.Join(",", channelIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => $"'{EscapeJavaScriptString(id)}'"));
            var result = await _web.CoreWebView2.ExecuteScriptAsync(BuildMentionCountScript(ids));
            return DiscordChannelMentionParser.Parse(DiscordScriptParsing.UnquoteJson(result));
        }

        private static string BuildMentionCountScript(string serializedIds)
        {
            return $@"
(() => {{
  const channelIds = [{serializedIds}];
  const counts = {{}};
{UnreadStoreLookupScript}
  for (const channelId of channelIds) {{
    try {{
      const unreadCount = Number(unreadStore?.getMentionCount(channelId));
      if (Number.isFinite(unreadCount)) {{ counts[channelId] = Math.max(0, unreadCount); continue; }}
    }} catch {{ }}
{DomBadgeLookupScript}
    counts[channelId] = count;
  }}
  return JSON.stringify(counts);
}})();";
        }
    }
}
