using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EpicRPGBot.UI.Puzzle.Local;

namespace PuzzleReplay
{
    internal static class ReplayPolicyValidation
    {
        public static void Revoke(string path, LocalPuzzlePolicy policy)
        {
            policy.ValidatedFingerprint = "";
            Write(path, policy);
        }

        public static int Seal(string path, LocalPuzzlePolicy policy, LocalPuzzleAnswerProvider provider,
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

        private static void Write(string path, LocalPuzzlePolicy policy) =>
            File.WriteAllText(path, JsonSerializer.Serialize(policy, new JsonSerializerOptions { WriteIndented = true }) + "\n");
    }
}
