using System;
using System.Globalization;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient
    {
        public async Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            try
            {
                var result = await _web.CoreWebView2.ExecuteScriptAsync(BuildCaptchaImageCandidateScript(messageId));
                var payload = DiscordScriptParsing.UnquoteJson(result);
                var url = NormalizeRemoteImageUrl(DiscordScriptParsing.UnquoteJson(DiscordScriptParsing.ExtractField(payload, "url")));
                return string.IsNullOrWhiteSpace(url) ? null : url;
            }
            catch
            {
                return null;
            }
        }

        public async Task<byte[]> CaptureMessageImagePngAsync(string messageId)
        {
            if (_web.CoreWebView2 == null || string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            try
            {
                var json = await _web.CoreWebView2.ExecuteScriptAsync(BuildCaptchaImageCandidateScript(messageId));
                var payload = DiscordScriptParsing.UnquoteJson(json);
                var ok = DiscordScriptParsing.ExtractField(payload, "ok");
                if (string.IsNullOrEmpty(ok) || ok.IndexOf("true", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return null;
                }

                var x = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "x"));
                var y = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "y"));
                var w = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "w"));
                var h = DiscordScriptParsing.ParseDouble(DiscordScriptParsing.ExtractField(payload, "h"));
                if (w < 2 || h < 2)
                {
                    return null;
                }

                var screenshotArgs =
                    $"{{\"format\":\"png\",\"fromSurface\":true,\"captureBeyondViewport\":true," +
                    $"\"clip\":{{\"x\":{Math.Max(0, x).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"y\":{Math.Max(0, y).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"width\":{Math.Max(2, w).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"height\":{Math.Max(2, h).ToString(CultureInfo.InvariantCulture)}," +
                    $"\"scale\":1}}}}";

                var response = await _web.CoreWebView2.CallDevToolsProtocolMethodAsync("Page.captureScreenshot", screenshotArgs);
                var data = DiscordScriptParsing.ExtractField(DiscordScriptParsing.UnquoteJson(response), "data");
                return string.IsNullOrWhiteSpace(data) ? null : Convert.FromBase64String(data);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildCaptchaImageCandidateScript(string messageId)
        {
            var escapedMessageId = messageId.Replace("\\", "\\\\").Replace("'", "\\'");
            return $@"
(() => {{
  const minimumWidth = 96;
  const minimumHeight = 48;
  const minimumArea = 6000;
  const root = document.getElementById('{escapedMessageId}');
  if (!root) return JSON.stringify({{ ok:false, url:'' }});
  const isImageUrl = (value) => {{
    const raw = (value || '').trim();
    if (!raw) return false;
    if (/^\/\//.test(raw)) return true;
    if (/^https?:\/\//i.test(raw)) return true;
    return /cdn\.discordapp\.(com|net)|media\.discordapp\.(net|com)|\/attachments\//i.test(raw) ||
      /\.(png|jpe?g|webp|gif)(\?|$)/i.test(raw);
  }};
  const normalizeUrl = (value) => {{
    const raw = (value || '').trim();
    if (!raw) return '';
    if (/^\/\//.test(raw)) return 'https:' + raw;
    if (/^https?:\/\//i.test(raw)) return raw;
    return '';
  }};
  const pickBestUrl = (img) => {{
    const anchor = img.closest('a[href]');
    const candidates = [
      anchor ? anchor.href : '',
      img.currentSrc || '',
      img.getAttribute('src') || '',
      img.src || '',
      img.getAttribute('data-safe-src') || ''
    ];
    for (const candidate of candidates) {{
      if (isImageUrl(candidate)) return normalizeUrl(candidate);
    }}
    return '';
  }};
  const imgs = Array.from(root.querySelectorAll('img'));
  let best = null;
  for (const img of imgs) {{
    const rect = img.getBoundingClientRect();
    const style = window.getComputedStyle(img);
    const visible = rect.width > 1 && rect.height > 1 && style.visibility !== 'hidden' && style.display !== 'none';
    if (!visible) continue;
    const area = rect.width * rect.height;
    if (rect.width < minimumWidth || rect.height < minimumHeight || area < minimumArea) continue;
    const url = pickBestUrl(img);
    if (!url) continue;
    const anchor = img.closest('a[href]');
    const score = area + (anchor ? 5000 : 0) + Math.min((img.naturalWidth || 0) * (img.naturalHeight || 0), 200000) / 50;
    if (!best || score > best.score) {{
      best = {{
        score,
        url,
        x: rect.left + window.scrollX,
        y: rect.top + window.scrollY,
        w: rect.width,
        h: rect.height
      }};
    }}
  }}
  if (!best) return JSON.stringify({{ ok:false, url:'' }});
  return JSON.stringify({{
    ok:true,
    url:best.url,
    x:best.x,
    y:best.y,
    w:best.w,
    h:best.h
  }});
}})();";
        }

        private static string NormalizeRemoteImageUrl(string rawUrl)
        {
            var value = (rawUrl ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.StartsWith("//", StringComparison.Ordinal))
            {
                value = "https:" + value;
            }

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                return string.Empty;
            }

            var scheme = uri.Scheme ?? string.Empty;
            if (!string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return uri.AbsoluteUri;
        }
    }
}
