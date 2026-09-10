using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PuzzleReplay
{
    internal static class ReplayReportWriter
    {
        public static void Write(string path, IReadOnlyList<ReplayPrediction> predictions)
        {
            var rows = new List<string>();
            var perLine = predictions.Count / 250 + 1;
            for (var start = 0; start < predictions.Count; start += perLine)
                rows.Add(string.Join(",", predictions.Skip(start).Take(perLine)
                    .Select(prediction => JsonSerializer.Serialize(prediction))));
            File.WriteAllText(path, "[\n" + string.Join(",\n", rows) + "\n]\n");
        }
    }
}
