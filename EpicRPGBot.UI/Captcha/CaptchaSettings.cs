using System;
using System.IO;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaSettings
    {
        private CaptchaSettings() { }
        public string ItemNamesFile { get; private set; }
        public string TemplateDirectory { get; private set; }
        public string PolicyFile { get; private set; }
        public string SelfTestReplayDirectory { get; private set; }
        public bool AutomaticAnswersEnabled { get; private set; }

        public static CaptchaSettings LoadDefault() => new CaptchaSettings
        {
            ItemNamesFile = ResolvePath(Env.Get("CAPTCHA_ITEM_NAMES_FILE", "items.json")),
            TemplateDirectory = ResolvePath(Env.Get("CAPTCHA_TEMPLATES_DIR", "Items")),
            PolicyFile = ResolvePath(Env.Get("CAPTCHA_LOCAL_POLICY_FILE", "captcha-local.json")),
            SelfTestReplayDirectory = ResolvePath(Env.Get("CAPTCHA_SELFTEST_REPLAY_DIR", "")),
            AutomaticAnswersEnabled = string.Equals(Env.Get("CAPTCHA_AUTO_SEND", "1"), "1", StringComparison.Ordinal)
        };

        public string Describe() => "mode=local, items=" + ItemNamesFile + ", templates=" + TemplateDirectory;

        private static string ResolvePath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) return "";
            if (Path.IsPathRooted(rawPath)) return rawPath.Trim();
            var current = AppDomain.CurrentDomain.BaseDirectory;
            for (var i = 0; i < 8; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(current, rawPath.Trim()));
                if (File.Exists(candidate) || Directory.Exists(candidate)) return candidate;
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath.Trim()));
        }
    }
}
