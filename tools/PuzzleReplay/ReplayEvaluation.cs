using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EpicRPGBot.UI.Puzzle.Local;

namespace PuzzleReplay
{
    internal static class ReplayEvaluation
    {
        private static readonly IBinomialIntervalEstimator Intervals = new WilsonIntervalEstimator();
        public static async Task<List<ReplayPrediction>> RunAsync(string manifest,
            IEnumerable<ReplayRecord> records, LocalPuzzleAnswerProvider provider)
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
                if (!predictions[predictions.Count - 1].Correct)
                    Console.WriteLine("Review index=" + record.Index + ", expected=" + record.Expected +
                        ", accepted=" + result.IsMatch + ", prediction=" + result.Label);
                if (predictions.Count % 25 == 0) Console.WriteLine("Evaluated " + predictions.Count +
                    ", correct=" + predictions.Count(prediction => prediction.Correct));
            }
            return predictions;
        }

        public static object Summary(List<ReplayPrediction> predictions, string fingerprint) => new
        {
            Fingerprint = fingerprint, Total = predictions.Count,
            Correct = predictions.Count(prediction => prediction.Correct),
            Accuracy95 = Intervals.Estimate(predictions.Count(prediction => prediction.Correct), predictions.Count),
            TopCandidateCorrect = predictions.Count(prediction =>
                prediction.Candidates.Count > 0 && prediction.Candidates[0].Label == prediction.Expected),
            Wrong = predictions.Count(prediction => prediction.Accepted && !prediction.Correct),
            Rejected = predictions.Count(prediction => !prediction.Accepted),
            MeanMilliseconds = predictions.Average(prediction => prediction.Milliseconds),
            P95Milliseconds = predictions.OrderBy(prediction => prediction.Milliseconds)
                .ElementAt((int)Math.Ceiling(predictions.Count * 0.95) - 1).Milliseconds,
            ByItem = predictions.GroupBy(prediction => prediction.Expected).Select(SummarizeGroup),
            ByGrayscale = predictions.GroupBy(prediction => prediction.Grayscale ? "grayscale" : "color").Select(SummarizeGroup),
            ByCondition = predictions.GroupBy(prediction => (prediction.Grayscale ? "grayscale" : "color") +
                (prediction.Lines ? "/lines" : "/no-lines")).Select(SummarizeGroup),
            ByLines = predictions.GroupBy(prediction => prediction.Lines ? "lines" : "no-lines").Select(SummarizeGroup)
        };

        private static object SummarizeGroup(IGrouping<string, ReplayPrediction> group) => new
        {
            Group = group.Key, Total = group.Count(), Correct = group.Count(prediction => prediction.Correct),
            Accuracy95 = Intervals.Estimate(group.Count(prediction => prediction.Correct), group.Count()),
            Wrong = group.Count(prediction => prediction.Accepted && !prediction.Correct),
            Rejected = group.Count(prediction => !prediction.Accepted)
        };
    }
}
