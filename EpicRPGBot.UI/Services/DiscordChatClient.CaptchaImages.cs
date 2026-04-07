using System;
using System.Globalization;
using System.Text.Json;
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
                var payload = ParseCaptchaImageCandidatePayload(result);
                var url = NormalizeRemoteImageUrl(payload?.Url);
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
                var payload = ParseCaptchaImageCandidatePayload(json);
                if (payload == null || !payload.Ok)
                {
                    return null;
                }

                var x = payload.X;
                var y = payload.Y;
                var w = payload.Width;
                var h = payload.Height;
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
  if (!root) return {{ ok:false, url:'' }};
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
  const extractBackgroundUrl = (element) => {{
    if (!element) return '';
    const style = window.getComputedStyle(element);
    const bg = (style && style.backgroundImage) || '';
    const match = bg.match(/url\((['""]?)(.*?)\1\)/i);
    return match && match[2] ? match[2].trim() : '';
  }};
  const isVisibleCandidate = (element) => {{
    if (!element) return false;
    const rect = element.getBoundingClientRect();
    const style = window.getComputedStyle(element);
    if (style.visibility === 'hidden' || style.display === 'none') return false;
    if (rect.width < minimumWidth || rect.height < minimumHeight) return false;
    if (rect.width * rect.height < minimumArea) return false;
    return true;
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
  const registerCandidate = (element, url, scoreBoost) => {{
    if (!element || !url || !isImageUrl(url) || !isVisibleCandidate(element)) return;
    const rect = element.getBoundingClientRect();
    const area = rect.width * rect.height;
    const score = area + (scoreBoost || 0);
    if (!best || score > best.score) {{
      best = {{
        score,
        url: normalizeUrl(url),
        x: rect.left + window.scrollX,
        y: rect.top + window.scrollY,
        w: rect.width,
        h: rect.height
      }};
    }}
  }};

  let best = null;
  const imgs = Array.from(root.querySelectorAll('img'));
  for (const img of imgs) {{
    const anchor = img.closest('a[href]');
    const naturalBoost = Math.min((img.naturalWidth || 0) * (img.naturalHeight || 0), 200000) / 50;
    registerCandidate(img, pickBestUrl(img), (anchor ? 5000 : 0) + naturalBoost);
  }}

  const anchors = Array.from(root.querySelectorAll('a[href]'));
  for (const anchor of anchors) {{
    registerCandidate(anchor, anchor.href || anchor.getAttribute('href') || '', 4000);
  }}

  const backgroundNodes = Array.from(root.querySelectorAll('div, span, section, article, a'));
  for (const element of backgroundNodes) {{
    registerCandidate(element, extractBackgroundUrl(element), 3000);
  }}
  if (!best) return {{ ok:false, url:'' }};
  return {{
    ok:true,
    url:best.url,
    x:best.x,
    y:best.y,
    w:best.w,
    h:best.h
  }};
}})();";
        }

        private static CaptchaImageCandidatePayload ParseCaptchaImageCandidatePayload(string rawResult)
        {
            var payload = DiscordScriptParsing.UnquoteJson(rawResult);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        return null;
                    }

                    var root = document.RootElement;
                    return new CaptchaImageCandidatePayload(
                        ReadBoolean(root, "ok"),
                        ReadString(root, "url"),
                        ReadDouble(root, "x"),
                        ReadDouble(root, "y"),
                        ReadDouble(root, "w"),
                        ReadDouble(root, "h"));
                }
            }
            catch
            {
                return null;
            }
        }

        private static bool ReadBoolean(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            return bool.TryParse(property.ToString(), out var value) && value;
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) ||
                property.ValueKind == JsonValueKind.Null ||
                property.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : property.ToString();
        }

        private static double ReadDouble(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return 0;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value))
            {
                return value;
            }

            return double.TryParse(property.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                ? value
                : 0;
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

        private sealed class CaptchaImageCandidatePayload
        {
            public CaptchaImageCandidatePayload(bool ok, string url, double x, double y, double width, double height)
            {
                Ok = ok;
                Url = url ?? string.Empty;
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public bool Ok { get; }

            public string Url { get; }

            public double X { get; }

            public double Y { get; }

            public double Width { get; }

            public double Height { get; }
        }
    }
}
