using System;
using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleQuestionRegionLocator
    {
        public Rect Locate(Mat colors, bool allowNarrowCard = false)
        {
            using (var letters = CreateLetterMask(colors))
            using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(21, 3)))
            using (var components = new Mat())
            using (var statistics = new Mat())
            using (var centers = new Mat())
            {
                Cv2.MorphologyEx(letters, letters, MorphTypes.Close, kernel);
                var count = Cv2.ConnectedComponentsWithStats(letters, components, statistics, centers);
                var textStart = colors.Width;
                for (var component = 1; component < count; component++)
                {
                    var left = statistics.At<int>(component, 0);
                    var width = statistics.At<int>(component, 2);
                    var height = statistics.At<int>(component, 3);
                    if (left >= 50 && width >= 120 && height >= 12 && height <= 55)
                        textStart = Math.Min(textStart, left);
                }
                if (textStart == colors.Width || textStart > 300)
                {
                    if (allowNarrowCard && colors.Width <= 180 && colors.Height == 200)
                        return new Rect(0, 12, colors.Width, 176);
                    throw new ArgumentException("Cannot locate the puzzle question text region.");
                }
                return new Rect(0, 5, textStart - 8, colors.Height - 10);
            }
        }

        private static Mat CreateLetterMask(Mat colors)
        {
            var mask = new Mat(colors.Size(), MatType.CV_8UC1, Scalar.All(0));
            for (var y = 5; y < colors.Rows - 5; y++)
                for (var x = 0; x < colors.Cols; x++)
                {
                    var pixel = colors.At<Vec3b>(y, x);
                    var minimum = Math.Min(pixel.Item0, Math.Min(pixel.Item1, pixel.Item2));
                    var maximum = Math.Max(pixel.Item0, Math.Max(pixel.Item1, pixel.Item2));
                    if (minimum > 180 && maximum - minimum < 30) mask.Set(y, x, (byte)255);
                }
            return mask;
        }
    }
}
