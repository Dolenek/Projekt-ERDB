using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient : IDiscordMessageNavigator
    {
        private const int MessageNavigationAttempts = 50;
        private const int MessageNavigationPollDelayMs = 200;

        public async Task<bool> NavigateToMessageAsync(
            DiscordMessageReference reference,
            CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null || reference?.IsComplete != true || reference.TabRole != _tabRole)
            {
                return false;
            }

            if (await TryRevealMessageAsync(reference.MessageElementId))
            {
                return true;
            }

            if (!DiscordMessagePermalink.TryBuild(reference, out var permalink))
            {
                return false;
            }

            var previousUrl = _web.CoreWebView2.Source;
            var navigationSucceeded = false;
            try
            {
                _web.CoreWebView2.Navigate(permalink);
                navigationSucceeded = await WaitForMessageAsync(
                    reference.MessageElementId,
                    cancellationToken);
                return navigationSucceeded;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (!navigationSucceeded)
                    RestorePreviousUrl(previousUrl, permalink);
            }
        }

        private async Task<bool> WaitForMessageAsync(string messageElementId, CancellationToken token)
        {
            for (var attempt = 0; attempt < MessageNavigationAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(MessageNavigationPollDelayMs, token);
                if (await TryRevealMessageAsync(messageElementId)) return true;
            }

            return false;
        }

        private void RestorePreviousUrl(string previousUrl, string attemptedPermalink)
        {
            if (_web.CoreWebView2 != null &&
                !string.IsNullOrWhiteSpace(previousUrl) &&
                !string.Equals(previousUrl, attemptedPermalink, StringComparison.OrdinalIgnoreCase))
            {
                _web.CoreWebView2.Navigate(previousUrl);
            }
        }

        private async Task<bool> TryRevealMessageAsync(string messageElementId)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(messageElementId))
            {
                return false;
            }

            try
            {
                var elementId = EscapeJavaScriptString(messageElementId);
                DiscordMessagePermalink.TryExtractSnowflake(messageElementId, out var snowflake);
                var messageId = EscapeJavaScriptString(snowflake);
                var result = await _web.CoreWebView2.ExecuteScriptAsync(BuildRevealMessageScript(elementId, messageId));
                return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string BuildRevealMessageScript(string elementId, string messageId)
        {
            return $@"
(() => {{
  const elementId = '{elementId}';
  const messageId = '{messageId}';
  const target = document.getElementById(elementId) ||
    (messageId ? document.getElementById(`chat-messages-${{messageId}}`) : null) ||
    (messageId ? document.querySelector(`[data-message-id=""${{messageId}}""]`) : null);
  if (!target) return false;
  try {{ target.scrollIntoView({{ block: 'center', inline: 'nearest', behavior: 'smooth' }}); }} catch (e) {{}}
  const previousOutline = target.style.outline;
  const previousOutlineOffset = target.style.outlineOffset;
  target.style.outline = '2px solid #22D3EE';
  target.style.outlineOffset = '3px';
  window.setTimeout(() => {{
    target.style.outline = previousOutline;
    target.style.outlineOffset = previousOutlineOffset;
  }}, 2400);
  return true;
}})();";
        }
    }
}
