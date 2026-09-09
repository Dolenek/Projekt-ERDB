using System;
using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleScene : IDisposable
    {
        private PuzzleScene(Mat colors)
        {
            Colors = new Mat();
            Cv2.CopyMakeBorder(colors, Colors, 5, 5, 5, 5, BorderTypes.Constant, Scalar.All(34));
            colors.Dispose();
            Binary = PuzzleForeground.CreateMask(Colors);
            ForegroundArea = Cv2.Sum(Binary).Val0;
            Gray = new Mat();
            Cv2.CvtColor(Colors, Gray, ColorConversionCodes.BGR2GRAY);
        }
        public Mat Colors { get; }
        public Mat Gray { get; }
        public Mat Binary { get; }
        public double ForegroundArea { get; }

        public static PuzzleScene Decode(byte[] bytes, bool clean = false, bool adaptive = false, bool crossingLines = false,
            bool fine = false)
        {
            if (bytes == null || bytes.Length == 0 || bytes.Length > 8 * 1024 * 1024)
                throw new ArgumentException("Empty or oversized puzzle attachment.");
            using (var source = Cv2.ImDecode(bytes, ImreadModes.Color))
            {
                if (source.Empty() || source.Width > 2048 || source.Height > 1024)
                    throw new ArgumentException("Invalid puzzle attachment dimensions.");
                var tall = source.Height >= 160 && source.Height <= 240 && source.Width >= 140;
                var compact = source.Height >= 68 && source.Height <= 102 && source.Width >= 350;
                if (!tall && !compact) throw new ArgumentException("Unsupported puzzle attachment layout.");
                using (var normalized = new Mat())
                {
                    var height = tall ? 200 : 85;
                    Cv2.Resize(source, normalized, new Size((int)Math.Round(source.Width * (double)height / source.Height), height));
                    if (clean) return CreateCleanScene(normalized, tall, adaptive, crossingLines, fine);
                    var bounds = tall ? new Rect(0, 12, Math.Min(normalized.Width, 145), 176)
                        : new Rect(0, 0, Math.Min(normalized.Width, 100), 85);
                    using (var region = new Mat(normalized, bounds))
                    {
                        var colors = new Mat();
                        Cv2.Resize(region, colors, new Size(), 0.5, 0.5, InterpolationFlags.Area);
                        return new PuzzleScene(colors);
                    }
                }
            }
        }

        public void Dispose() { Gray.Dispose(); Colors.Dispose(); Binary.Dispose(); }

        private static PuzzleScene CreateCleanScene(Mat normalized, bool tall, bool adaptive, bool crossingLines, bool fine)
        {
            IPuzzleInterferenceFilter filter = crossingLines ? (IPuzzleInterferenceFilter)new PuzzleCrossingLineFilter(fine ? 1.5 : 3)
                : new PuzzleColoredLineFilter();
            return new PuzzleScene(new PuzzleCardPreprocessor(filter).Prepare(normalized, tall, adaptive, fine));
        }
    }
}
