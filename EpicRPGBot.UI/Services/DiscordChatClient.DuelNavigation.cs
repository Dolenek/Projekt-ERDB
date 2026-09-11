using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        private const int DuelHistoryLoadAttempts = 20;
        private const int DuelHistoryScanCount = 150;

        public async Task<IReadOnlyList<DiscordChannelReference>> DiscoverCategoryChannelsAsync(
            string categoryId,
            CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(categoryId))
            {
                return Array.Empty<DiscordChannelReference>();
            }

            cancellationToken.ThrowIfCancellationRequested();
            await ExpandDiscordCategoryAsync(categoryId);
            await Task.Delay(300, cancellationToken);
            var payload = await _web.CoreWebView2.ExecuteScriptAsync(BuildChannelDiscoveryScript());
            return ParseChannelReferences(DiscordScriptParsing.UnquoteJson(payload));
        }

        public async Task<bool> NavigateToChannelAndWaitAsync(
            string url,
            CancellationToken cancellationToken = default)
        {
            if (!DuelChannelCatalog.IsAllowedTargetUrl(url))
            {
                ReportTelemetry($"Blocked navigation outside the fixed duel scope: {url}");
                return false;
            }

            ReportTelemetry($"Visiting {DuelChannelCatalog.DescribeTargetUrl(url)}: {url}");
            await NavigateToChannelAsync(url);
            await Task.Delay(250, cancellationToken);
            for (var attempt = 0; attempt < 24; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await IsDiscordChannelReadyAsync(url))
                {
                    await ScrollToLatestMessagesAsync();
                    await Task.Delay(150, cancellationToken);
                    return true;
                }

                await Task.Delay(250, cancellationToken);
            }

            return false;
        }

        public async Task<IReadOnlyList<DiscordMessageSnapshot>> GetMessagesSinceAsync(
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken = default)
        {
            var snapshots = new Dictionary<string, DiscordMessageSnapshot>(StringComparer.Ordinal);
            for (var attempt = 0; attempt < DuelHistoryLoadAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var batch = await GetRecentMessagesAsync(DuelHistoryScanCount);
                foreach (var snapshot in batch.Where(item => item != null && !string.IsNullOrWhiteSpace(item.Id)))
                {
                    snapshots[snapshot.Id] = snapshot;
                }

                if (HasReachedCutoff(batch, cutoffUtc) || !await ScrollToOlderMessagesAsync())
                {
                    break;
                }

                await Task.Delay(400, cancellationToken);
            }

            return snapshots.Values
                .Where(item => !item.CreatedAtUtc.HasValue || item.CreatedAtUtc.Value >= cutoffUtc)
                .OrderBy(item => item.CreatedAtUtc ?? DateTimeOffset.MaxValue)
                .ToArray();
        }

        private static bool HasReachedCutoff(
            IReadOnlyList<DiscordMessageSnapshot> snapshots,
            DateTimeOffset cutoffUtc)
        {
            return snapshots.Any(item => item?.CreatedAtUtc.HasValue == true && item.CreatedAtUtc.Value <= cutoffUtc);
        }

        private async Task ExpandDiscordCategoryAsync(string categoryId)
        {
            var escapedId = EscapeJavaScriptString(categoryId);
            var script = $@"
(() => {{
  const target = Array.from(document.querySelectorAll('[data-list-item-id], [id]'))
    .find(node => (node.getAttribute('data-list-item-id') || node.id || '').includes('{escapedId}'));
  if (!target) return false;
  const clickable = target.closest('button, [role=""button""]') || target.querySelector('button, [role=""button""]') || target;
  if (clickable.getAttribute('aria-expanded') === 'false') clickable.click();
  return true;
}})();";
            await _web.CoreWebView2.ExecuteScriptAsync(script);
        }

        private async Task<bool> IsDiscordChannelReadyAsync(string url)
        {
            var expected = EscapeJavaScriptString(new Uri(url).AbsolutePath.TrimEnd('/'));
            var script = $@"
(() => {{
  const readyPath = location.pathname.replace(/\/$/, '') === '{expected}';
  const hasChat = document.querySelector('main, [role=""main""]') !== null;
  return readyPath && hasChat;
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> ScrollToOlderMessagesAsync()
        {
            const string script = @"
(() => {
  const first = document.querySelector('li[id^=""chat-messages-""]');
  if (!first) return false;
  const scroller = first.closest('[class*=""scroller""]');
  if (!scroller || scroller.scrollTop <= 0) return false;
  scroller.scrollTop = 0;
  return true;
})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        private Task<string> ScrollToLatestMessagesAsync()
        {
            const string script = @"
(() => {
  const last = Array.from(document.querySelectorAll('li[id^=""chat-messages-""]')).pop();
  if (!last) return false;
  const scroller = last.closest('[class*=""scroller""]');
  if (!scroller) return false;
  scroller.scrollTop = scroller.scrollHeight;
  return true;
})();";
            return _web.CoreWebView2.ExecuteScriptAsync(script);
        }

        private static string BuildChannelDiscoveryScript()
        {
            return @"
(() => {
  const currentGuild = location.pathname.match(/^\/channels\/(\d+)/)?.[1] || '';
  const selector = [
    'a[href*=""/channels/""]',
    '[data-list-item-id*=""channel""]',
    '[role=""treeitem""]',
    '[role=""link""]'
  ].join(',');
  const knownChannel = /road-to-(?:50|200|500|1500|3000|6000\s*\+)|dueling-[1-4]/i;
  const channels = new Map();
  for (const element of document.querySelectorAll(selector)) {
    const link = element.matches('a[href*=""/channels/""]')
      ? element
      : element.querySelector('a[href*=""/channels/""]');
    const href = link?.getAttribute('href') || element.getAttribute('href') || '';
    const hrefMatch = href.match(/^\/channels\/(\d+)\/(\d+)/);
    const identity = [
      element.getAttribute('data-list-item-id') || '', element.id || '',
      link?.getAttribute('data-list-item-id') || '', link?.id || ''
    ].join(' ');
    const identityMatch = identity.match(/(?:channels?[_:-]+|channel-)(\d{15,22})/i);
    const guildId = hrefMatch?.[1] || currentGuild;
    const channelId = hrefMatch?.[2] || identityMatch?.[1] || '';
    const rawName = [
      element.getAttribute('data-dnd-name'), element.getAttribute('aria-label'),
      element.getAttribute('title'), link?.getAttribute('aria-label'), element.innerText
    ].filter(Boolean).join(' ');
    const name = (rawName.match(knownChannel)?.[0] || '').replace(/\s+/g, '').toLowerCase();
    if (!channelId || !name || !guildId || (currentGuild && guildId !== currentGuild)) continue;
    channels.set(name, { id: channelId, name, url: `${location.origin}/channels/${guildId}/${channelId}` });
  }
  return JSON.stringify(Array.from(channels.values()));
})();";
        }

        private static IReadOnlyList<DiscordChannelReference> ParseChannelReferences(string payload)
        {
            return DiscordChannelReferenceParser.Parse(payload);
        }
    }
}
