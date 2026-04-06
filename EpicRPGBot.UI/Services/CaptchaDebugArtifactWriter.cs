using System;
using System.IO;
using System.Text.Json;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaDebugArtifactWriter
    {
        private readonly bool _enabled;
        private readonly string _outputDirectory;

        public CaptchaDebugArtifactWriter()
        {
            _enabled = !string.Equals(Env.Get("CAPTCHA_DEBUG_CAPTURE", "0"), "0", StringComparison.OrdinalIgnoreCase);
            _outputDirectory = ResolveOutputDirectory();
        }

        public string TryWriteCapture(string messageId, string source, string url, byte[] bytes)
        {
            if (!_enabled || bytes == null || bytes.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                Directory.CreateDirectory(_outputDirectory);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
                var safeMessageId = Sanitize(messageId);
                var safeSource = Sanitize(source);
                var extension = DetectExtension(bytes, url);
                var fileName = $"{timestamp}_{safeMessageId}_{safeSource}{extension}";
                var imagePath = Path.Combine(_outputDirectory, fileName);
                File.WriteAllBytes(imagePath, bytes);

                var metadataPath = Path.ChangeExtension(imagePath, ".json");
                var metadata = new
                {
                    messageId,
                    source,
                    url,
                    byteCount = bytes.Length,
                    savedUtc = DateTime.UtcNow.ToString("O")
                };

                File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    WriteIndented = true
                }));

                return imagePath;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string DetectExtension(byte[] bytes, string url)
        {
            var mediaType = EpicRPGBot.UI.Captcha.CaptchaImageMediaTypeDetector.Detect(bytes);
            if (string.Equals(mediaType, "image/png", StringComparison.OrdinalIgnoreCase))
            {
                return ".png";
            }

            if (string.Equals(mediaType, "image/webp", StringComparison.OrdinalIgnoreCase))
            {
                return ".webp";
            }

            if ((url ?? string.Empty).IndexOf(".jpg", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (url ?? string.Empty).IndexOf(".jpeg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ".jpg";
            }

            return ".bin";
        }

        private static string ResolveOutputDirectory()
        {
            var raw = Env.Get("CAPTCHA_DEBUG_DIR", "artifacts/captcha-debug");
            if (Path.IsPathRooted(raw))
            {
                return raw;
            }

            var current = AppDomain.CurrentDomain.BaseDirectory;
            for (var i = 0; i < 8; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(current, raw));
                if (Directory.Exists(candidate) || File.Exists(Path.Combine(candidate, ".keep")))
                {
                    return candidate;
                }

                var parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, raw));
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unknown";
            }

            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }
    }
}
