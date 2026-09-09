using System;
using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal static class PuzzleLuminance
    {
        public static Mat MaximumChannel(Mat colors)
        {
            var maximum = new Mat(colors.Size(), MatType.CV_8UC1);
            for (var row = 0; row < colors.Rows; row++)
                for (var column = 0; column < colors.Cols; column++)
                {
                    var pixel = colors.At<Vec3b>(row, column);
                    maximum.Set(row, column, Math.Max(pixel.Item0, Math.Max(pixel.Item1, pixel.Item2)));
                }
            return maximum;
        }

        public static bool IsGrayscale(Mat colors, Mat foreground)
        {
            double chroma = 0;
            var count = 0;
            for (var row = 0; row < colors.Rows; row++)
                for (var column = 0; column < colors.Cols; column++)
                {
                    if (foreground.At<float>(row, column) == 0) continue;
                    var pixel = colors.At<Vec3b>(row, column);
                    chroma += Math.Max(pixel.Item0, Math.Max(pixel.Item1, pixel.Item2)) -
                        Math.Min(pixel.Item0, Math.Min(pixel.Item1, pixel.Item2));
                    count++;
                }
            return count > 0 && chroma / count < 18;
        }
    }
}
