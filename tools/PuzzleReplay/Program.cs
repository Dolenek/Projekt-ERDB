using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EpicRPGBot.UI.Puzzle;
using EpicRPGBot.UI.Puzzle.Local;

namespace PuzzleReplay
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: PuzzleReplay.exe <repository> <manifest> <output-json> [--validate] [--policy <path>] [--templates <directory>] [--allow-unsupported]");
                return 2;
            }
            try { return await RunAsync(args); }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        }

        private static async Task<int> RunAsync(string[] args)
        {
            var options = ReplayOptions.Parse(args);
            var root = options.Root;
            var manifest = options.Manifest;
            var validate = options.Validate;
            var policyPath = options.PolicyPath;
            var policy = LocalPuzzlePolicy.Load(policyPath);
            if (validate) ReplayPolicyValidation.Revoke(policyPath, policy);
            var records = ReplayDataset.Read(manifest);
            if (validate) ReplayDataset.EnsureHoldoutSeparation(manifest, records);
            using (var provider = new LocalPuzzleAnswerProvider(options.TemplateDirectory,
                PuzzleItemCatalog.Load(Path.Combine(root, "items.json")), policy))
            {
                if (!options.AllowUnsupported && records.Any(record => !provider.Labels.Contains(record.Expected)))
                    throw new InvalidDataException("Replay manifest contains an unsupported target label.");
                var predictions = await ReplayEvaluation.RunAsync(manifest, records, provider);
                ReplayReportWriter.Write(options.Output, predictions);
                var summary = JsonSerializer.Serialize(ReplayEvaluation.Summary(predictions, provider.Fingerprint));
                File.WriteAllText(Path.ChangeExtension(options.Output, ".summary.json"), summary + "\n");
                Console.WriteLine(summary);
                if (validate) return ReplayPolicyValidation.Seal(policyPath, policy, provider, records, predictions);
            }
            return 0;
        }

    }
}
