using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaColoredLineFilter : ICaptchaInterferenceFilter
    {
        public Mat Apply(Mat colors)
        {
            using (var removal = new Mat(colors.Size(), MatType.CV_8UC1, Scalar.All(0)))
            {
                foreach (var code in Palette(colors)) MarkThinComponents(colors, code, removal);
                if (Cv2.CountNonZero(removal) == 0) return colors.Clone();
                using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
                    Cv2.Dilate(removal, removal, kernel);
                var cleaned = new Mat();
                Cv2.Inpaint(colors, removal, cleaned, 3, InpaintTypes.Telea);
                return cleaned;
            }
        }

        private static HashSet<int> Palette(Mat colors)
        {
            var palette = new HashSet<int>();
            for (var y = 0; y < colors.Rows; y++)
                for (var x = 0; x < colors.Cols; x++)
                {
                    var pixel = colors.At<Vec3b>(y, x);
                    if (Chroma(pixel) > 60) palette.Add(Code(pixel));
                }
            return palette;
        }

        private static void MarkThinComponents(Mat colors, int code, Mat removal)
        {
            using (var mask = new Mat(colors.Size(), MatType.CV_8UC1, Scalar.All(0)))
            using (var components = new Mat())
            using (var statistics = new Mat())
            using (var centers = new Mat())
            {
                for (var y = 0; y < colors.Rows; y++)
                    for (var x = 0; x < colors.Cols; x++)
                    {
                        var pixel = colors.At<Vec3b>(y, x);
                        if (Code(pixel) == code && Chroma(pixel) > 60) mask.Set(y, x, (byte)255);
                    }
                var count = Cv2.ConnectedComponentsWithStats(mask, components, statistics, centers);
                for (var component = 1; component < count; component++)
                {
                    if (statistics.At<int>(component, 4) < 12) continue;
                    var points = ComponentPoints(components, component);
                    var size = Cv2.MinAreaRect(points).Size;
                    if (Math.Max(size.Width, size.Height) < 18 ||
                        Math.Max(size.Width, size.Height) / (Math.Min(size.Width, size.Height) + 1) < 8) continue;
                    foreach (var point in points) removal.Set(point.Y, point.X, (byte)255);
                }
            }
        }

        private static List<Point> ComponentPoints(Mat components, int component)
        {
            var points = new List<Point>();
            for (var y = 0; y < components.Rows; y++)
                for (var x = 0; x < components.Cols; x++)
                    if (components.At<int>(y, x) == component) points.Add(new Point(x, y));
            return points;
        }

        private static int Chroma(Vec3b pixel) => Math.Max(pixel.Item0, Math.Max(pixel.Item1, pixel.Item2)) -
            Math.Min(pixel.Item0, Math.Min(pixel.Item1, pixel.Item2));
        private static int Code(Vec3b pixel) => pixel.Item0 / 32 + 8 * (pixel.Item1 / 32) + 64 * (pixel.Item2 / 32);
    }
}

