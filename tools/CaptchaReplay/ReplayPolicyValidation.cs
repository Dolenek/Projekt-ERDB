using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicRPGBot.UI.Captcha.Local;

namespace CaptchaReplay
{
    internal static class ReplayPolicyValidation
    {
        public static void Revoke(string path, LocalCaptchaPolicy policy)
        {
            policy.ValidatedFingerprint = "";
            Write(path, policy);
        }

        public static int Seal(string path, LocalCaptchaPolicy policy, LocalCaptchaAnswerProvider provider,
            List<ReplayRecord> records, List<ReplayPrediction> predictions)
        {
            policy.TestTotal = records.Count;
            policy.TestCorrect = predictions.Count(prediction => prediction.Correct);
            policy.TestWrong = predictions.Count(prediction => prediction.Accepted && !prediction.Correct);
            policy.TestClassCounts = records.GroupBy(record => record.Expected).ToDictionary(group => group.Key, group => group.Count());
            // Reuse the production gate; a replay report cannot relax its requirements.
            var passed = predictions.Count == records.Count && policy.HasSufficientEvidence(provider.Labels);
            policy.ValidatedFingerprint = passed ? provider.Fingerprint : "";
            Write(path, policy);
            Console.WriteLine(passed ? "VALIDATED: automatic answers eligible." : "NOT VALIDATED: automatic answers remain disabled.");
            return passed ? 0 : 3;
        }

        private static void Write(string path, LocalCaptchaPolicy policy) =>
            File.WriteAllText(path, JsonSerializer.Serialize(policy, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}
