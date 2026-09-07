using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EpicRPGBot.UI.Captcha.Local
{
    public sealed class LocalCaptchaPolicy
    {
        public const string CurrentPipeline = "template-correlation-v1";
        public string Pipeline { get; set; } = CurrentPipeline;
        public double MinimumScore { get; set; } = 0.8;
        public double MinimumMargin { get; set; } = 0.08;
        public string ValidatedFingerprint { get; set; } = "";
        public int TestTotal { get; set; }
        public int TestCorrect { get; set; }
        public int TestWrong { get; set; }
        public Dictionary<string, int> TestClassCounts { get; set; } = new Dictionary<string, int>();

        public static LocalCaptchaPolicy Load(string path)
        {
            var policy = JsonSerializer.Deserialize<LocalCaptchaPolicy>(File.ReadAllText(path));
            if (policy == null || policy.Pipeline != CurrentPipeline ||
                !IsUnitValue(policy.MinimumScore) || !IsUnitValue(policy.MinimumMargin))
                throw new InvalidDataException("Invalid local captcha policy.");
            return policy;
        }

        public bool AllowsAutomaticAnswers(string templateFingerprint, IEnumerable<string> labels)
        {
            return TestTotal >= 100 && TestWrong == 0 && TestCorrect <= TestTotal &&
                TestCorrect >= Math.Ceiling(TestTotal * 0.9) &&
                TestClassCounts != null && TestClassCounts.Values.Sum() == TestTotal &&
                labels.All(label => TestClassCounts.TryGetValue(label, out var count) && count >= 5) &&
                string.Equals(ValidatedFingerprint, Fingerprint(templateFingerprint), StringComparison.Ordinal);
        }

        public string Fingerprint(string templateFingerprint)
        {
            var settings = string.Join("|", CurrentPipeline, templateFingerprint,
                MinimumScore.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                MinimumMargin.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(settings))).Replace("-", "").ToLowerInvariant();
        }

        private static bool IsUnitValue(double value) => !double.IsNaN(value) && value > 0 && value <= 1;
    }
}
