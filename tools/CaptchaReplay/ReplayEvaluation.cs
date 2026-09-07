using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EpicRPGBot.UI.Captcha.Local;

namespace CaptchaReplay
{
    internal static class ReplayEvaluation
    {
        public static async Task<List<ReplayPrediction>> RunAsync(string manifest,
            IEnumerable<ReplayRecord> records, LocalCaptchaAnswerProvider provider)
        {
            var predictions = new List<ReplayPrediction>();
            foreach (var record in records)
            {
                var watch = Stopwatch.StartNew();
                var result = await provider.SolveAsync(ReplayDataset.ReadImage(Path.GetDirectoryName(manifest), record), default);
                predictions.Add(new ReplayPrediction
                {
                    Index = record.Index, Expected = record.Expected, Predicted = result.Label,
                    Accepted = result.IsMatch, Correct = result.IsMatch && result.Label == record.Expected,
                    Lines = record.Lines, Grayscale = record.Grayscale, Milliseconds = watch.ElapsedMilliseconds,
                    Candidates = result.Candidates, Detail = result.Detail
                });
                if (predictions.Count % 25 == 0) Console.WriteLine("Evaluated " + predictions.Count);
            }
            return predictions;
        }

        public static object Summary(List<ReplayPrediction> predictions, string fingerprint) => new
        {
            Fingerprint = fingerprint, Total = predictions.Count,
            Correct = predictions.Count(prediction => prediction.Correct),
            Wrong = predictions.Count(prediction => prediction.Accepted && !prediction.Correct),
            Rejected = predictions.Count(prediction => !prediction.Accepted),
            MeanMilliseconds = predictions.Average(prediction => prediction.Milliseconds),
            P95Milliseconds = predictions.OrderBy(prediction => prediction.Milliseconds)
                .ElementAt((int)Math.Ceiling(predictions.Count * 0.95) - 1).Milliseconds,
            ByItem = predictions.GroupBy(prediction => prediction.Expected).Select(SummarizeGroup),
            ByGrayscale = predictions.GroupBy(prediction => prediction.Grayscale ? "grayscale" : "color").Select(SummarizeGroup),
            ByLines = predictions.GroupBy(prediction => prediction.Lines ? "lines" : "no-lines").Select(SummarizeGroup)
        };

        private static object SummarizeGroup(IGrouping<string, ReplayPrediction> group) => new
        {
            Group = group.Key, Total = group.Count(), Correct = group.Count(prediction => prediction.Correct),
            Wrong = group.Count(prediction => prediction.Accepted && !prediction.Correct),
            Rejected = group.Count(prediction => !prediction.Accepted)
        };
    }
}
