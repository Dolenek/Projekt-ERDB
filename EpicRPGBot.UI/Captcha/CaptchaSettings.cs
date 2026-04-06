using System;
using System.IO;
namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaSettings
    {
        private CaptchaSettings(
            string openAiApiKey,
            string openAiModel,
            string openAiRetryModel,
            int apiTimeoutSeconds,
            string itemNamesFile,
            string selfTestReplayDirectory)
        {
            OpenAiApiKey = openAiApiKey ?? string.Empty;
            OpenAiModel = string.IsNullOrWhiteSpace(openAiModel) ? "gpt-5.4-mini" : openAiModel.Trim();
            OpenAiRetryModel = string.IsNullOrWhiteSpace(openAiRetryModel) ? "gpt-5" : openAiRetryModel.Trim();
            ApiTimeoutSeconds = Math.Max(1, apiTimeoutSeconds);
            ItemNamesFile = ResolvePath(itemNamesFile);
            SelfTestReplayDirectory = ResolvePath(selfTestReplayDirectory);
        }

        public string OpenAiApiKey { get; }

        public string OpenAiModel { get; }

        public string OpenAiRetryModel { get; }

        public int ApiTimeoutSeconds { get; }

        public string ItemNamesFile { get; }

        public string SelfTestReplayDirectory { get; }

        public static CaptchaSettings LoadDefault()
        {
            return new CaptchaSettings(
                Env.Get("CAPTCHA_OPENAI_API_KEY", null),
                Env.Get("CAPTCHA_OPENAI_MODEL", "gpt-5.4-mini"),
                Env.Get("CAPTCHA_OPENAI_RETRY_MODEL", "gpt-5"),
                ReadPositiveInt("CAPTCHA_API_TIMEOUT_SECONDS", 10, 120),
                Env.Get("CAPTCHA_ITEM_NAMES_FILE", null),
                Env.Get("CAPTCHA_SELFTEST_REPLAY_DIR", null));
        }

        public string Describe()
        {
            return $"mode=openai, model={OpenAiModel}, retryModel={OpenAiRetryModel}, items={ItemNamesFile}, timeout={ApiTimeoutSeconds}s";
        }

        private static int ReadPositiveInt(string key, int fallback, int maxValue)
        {
            try
            {
                var raw = Env.Get(key, null);
                if (!string.IsNullOrWhiteSpace(raw) &&
                    int.TryParse(raw, out var parsed) &&
                    parsed > 0)
                {
                    return Math.Min(parsed, maxValue);
                }
            }
            catch
            {
            }

            return fallback;
        }

        private static string ResolvePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            var trimmed = rawPath.Trim();
            if (Path.IsPathRooted(trimmed))
            {
                return trimmed;
            }

            var current = AppDomain.CurrentDomain.BaseDirectory;
            for (var i = 0; i < 8; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(current, trimmed));
                if (File.Exists(candidate) || Directory.Exists(candidate))
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

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, trimmed));
        }
    }
}
