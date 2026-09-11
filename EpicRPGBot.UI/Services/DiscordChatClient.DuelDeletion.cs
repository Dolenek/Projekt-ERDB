using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<bool> DeleteOwnMessageAsync(
            string messageId,
            CancellationToken cancellationToken = default)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(messageId))
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();
            ReportTelemetry($"Deleting own message {messageId}.");
            if (!await RetryActionAsync(() => OpenMessageMenuAsync(messageId), cancellationToken))
            {
                ReportTelemetry($"Delete failed: message action menu did not open for {messageId}.");
                return false;
            }

            if (!await RetryActionAsync(ClickDeleteMenuItemAsync, cancellationToken))
            {
                ReportTelemetry($"Delete failed: Delete Message was not found for {messageId}.");
                return false;
            }

            var deleted = await ConfirmAndWaitForDeletionAsync(messageId, cancellationToken);
            if (!deleted)
            {
                ReportTelemetry($"Delete failed: message {messageId} remained visible after confirmation.");
            }

            return deleted;
        }

        private async Task<bool> OpenMessageMenuAsync(string messageId)
        {
            var escapedId = EscapeJavaScriptString(messageId);
            var script = $@"
(() => {{
  const messageId = '{escapedId}';
  const numericId = messageId.match(/\d{{15,22}}$/)?.[0] || '';
  const root = document.getElementById(messageId) ||
    (numericId ? document.querySelector(`[data-message-id=""${{numericId}}""]`) : null);
  if (!root) return false;
  try {{ root.scrollIntoView({{ block: 'center', inline: 'nearest' }}); }} catch (e) {{}}
  ['pointerover', 'mouseover', 'mouseenter', 'mousemove'].forEach(type =>
    root.dispatchEvent(new MouseEvent(type, {{ bubbles: true, view: window }})));
  const rootRect = root.getBoundingClientRect();
  const button = Array.from(document.querySelectorAll('button')).find(candidate => {{
    const label = `${{candidate.getAttribute('aria-label') || ''}} ${{candidate.getAttribute('title') || ''}}`.toLowerCase();
    if (!label.includes('more') && !label.includes('další')) return false;
    const rect = candidate.getBoundingClientRect();
    return root.contains(candidate) || (rect.top >= rootRect.top - 30 && rect.top <= rootRect.bottom + 30);
  }});
  if (!button) return false;
  button.click();
  return true;
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return IsTrue(result);
        }

        private Task<bool> ClickDeleteMenuItemAsync()
        {
            return ClickVisibleElementByTextAsync("delete message", "smazat zprávu");
        }

        private Task<bool> ConfirmMessageDeletionAsync()
        {
            return ClickVisibleElementByTextAsync("delete", "smazat");
        }

        private async Task<bool> ConfirmAndWaitForDeletionAsync(
            string messageId,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 12; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await MessageExistsAsync(messageId))
                {
                    return true;
                }

                await ConfirmMessageDeletionAsync();
                await Task.Delay(150, cancellationToken);
            }

            return !await MessageExistsAsync(messageId);
        }

        private async Task<bool> RetryActionAsync(
            Func<Task<bool>> action,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 10; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (await action())
                {
                    return true;
                }

                await Task.Delay(150, cancellationToken);
            }

            return false;
        }

        private async Task<bool> MessageExistsAsync(string messageId)
        {
            var escapedId = EscapeJavaScriptString(messageId);
            var script = $@"
(() => {{
  const messageId = '{escapedId}';
  const numericId = messageId.match(/\d{{15,22}}$/)?.[0] || '';
  return document.getElementById(messageId) !== null ||
    Boolean(numericId && document.querySelector(`[data-message-id=""${{numericId}}""]`));
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return IsTrue(result);
        }

        private async Task<bool> ClickVisibleElementByTextAsync(params string[] labels)
        {
            var serialized = string.Join(",", Array.ConvertAll(
                labels,
                label => $"'{EscapeJavaScriptString(label)}'"));
            var script = $@"
(() => {{
  const labels = [{serialized}];
  const elements = Array.from(document.querySelectorAll('[role=""menuitem""], [role=""dialog""] button'));
  const target = elements.find(element => labels.includes((element.innerText || '').trim().toLowerCase()));
  if (!target) return false;
  target.click();
  return true;
}})();";
            var result = await _web.CoreWebView2.ExecuteScriptAsync(script);
            return IsTrue(result);
        }

        private static bool IsTrue(string result)
        {
            return string.Equals(result?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }
    }
}
