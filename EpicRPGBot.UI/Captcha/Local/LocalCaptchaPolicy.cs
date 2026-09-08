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
        public const string RefinedPipeline = "template-refinement-v2";
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
            if (policy == null || !IsSupportedPipeline(policy.Pipeline) ||
                !IsUnitValue(policy.MinimumScore) || !IsUnitValue(policy.MinimumMargin))
                throw new InvalidDataException("Invalid local captcha policy.");
            return policy;
        }

        public bool AllowsAutomaticAnswers(string templateFingerprint, IEnumerable<string> labels)
        {
            return HasSufficientEvidence(labels) &&
                string.Equals(ValidatedFingerprint, Fingerprint(templateFingerprint), StringComparison.Ordinal);
        }

        public bool HasSufficientEvidence(IEnumerable<string> labels)
        {
            return IsSupportedPipeline(Pipeline) && IsUnitValue(MinimumScore) && IsUnitValue(MinimumMargin) &&
                TestTotal >= 100 && TestWrong == 0 && TestCorrect <= TestTotal &&
                TestCorrect >= Math.Ceiling(TestTotal * 0.9) &&
                TestClassCounts != null && TestClassCounts.Values.All(count => count >= 5) &&
                TestClassCounts.Values.Sum(count => (long)count) == TestTotal &&
                TestClassCounts.Count == labels.Distinct().Count() &&
                labels.All(label => TestClassCounts.TryGetValue(label, out var count) && count >= 5);
        }

        public string Fingerprint(string templateFingerprint)
        {
            var settings = string.Join("|", Pipeline, templateFingerprint,
                MinimumScore.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                MinimumMargin.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(settings))).Replace("-", "").ToLowerInvariant();
        }

        private static bool IsUnitValue(double value) => !double.IsNaN(value) && value > 0 && value <= 1;
        public static bool IsSupportedPipeline(string pipeline) =>
            pipeline == CurrentPipeline || pipeline == RefinedPipeline;
    }
}
