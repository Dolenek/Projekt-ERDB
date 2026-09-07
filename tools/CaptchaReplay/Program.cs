using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EpicRPGBot.UI.Captcha;
using EpicRPGBot.UI.Captcha.Local;

namespace CaptchaReplay
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: CaptchaReplay.exe <repository> <manifest> <output-json> [--validate]");
                return 2;
            }
            try { return await RunAsync(args); }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }

        private static async Task<int> RunAsync(string[] args)
        {
            var root = Path.GetFullPath(args[0]);
            var manifest = Path.GetFullPath(args[1]);
            var records = ReplayDataset.Read(manifest);
            var validate = args.Contains("--validate");
            if (validate) ReplayDataset.EnsureHoldoutSeparation(manifest, records);
            var policyPath = Path.Combine(root, "captcha-local.json");
            var policy = LocalCaptchaPolicy.Load(policyPath);
            using (var provider = new LocalCaptchaAnswerProvider(Path.Combine(root, "Items"),
                CaptchaItemCatalog.Load(Path.Combine(root, "items.json")), policy))
            {
                var predictions = await ReplayEvaluation.RunAsync(manifest, records, provider);
                File.WriteAllText(args[2], "[\n" + string.Join(",\n", predictions.Select(prediction => JsonSerializer.Serialize(prediction))) + "\n]\n");
                var summary = JsonSerializer.Serialize(ReplayEvaluation.Summary(predictions, provider.Fingerprint));
                File.WriteAllText(Path.ChangeExtension(args[2], ".summary.json"), summary + "\n");
                Console.WriteLine(summary);
                if (validate) return Seal(policyPath, policy, provider.Fingerprint, records, predictions);
            }
            return 0;
        }

        private static int Seal(string path, LocalCaptchaPolicy policy, string fingerprint,
            System.Collections.Generic.List<ReplayRecord> records,
            System.Collections.Generic.List<ReplayPrediction> predictions)
        {
            policy.TestTotal = records.Count;
            policy.TestCorrect = predictions.Count(prediction => prediction.Correct);
            policy.TestWrong = predictions.Count(prediction => prediction.Accepted && !prediction.Correct);
            policy.TestClassCounts = records.GroupBy(record => record.Expected).ToDictionary(group => group.Key, group => group.Count());
            var passed = policy.TestTotal >= 100 && policy.TestWrong == 0 &&
                policy.TestCorrect >= Math.Ceiling(policy.TestTotal * 0.9) &&
                policy.TestClassCounts.Count == 15 && policy.TestClassCounts.Values.All(count => count >= 5);
            policy.ValidatedFingerprint = passed ? fingerprint : "";
            File.WriteAllText(path, JsonSerializer.Serialize(policy, new JsonSerializerOptions { WriteIndented = true }) + "\n");
            Console.WriteLine(passed ? "VALIDATED: automatic answers eligible." : "NOT VALIDATED: automatic answers remain disabled.");
            return passed ? 0 : 3;
        }
    }
}
