using System;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaCrossingLineFilter : ICaptchaInterferenceFilter
    {
        private readonly double _thinRatio;
        public CaptchaCrossingLineFilter(double thinRatio = 3) { _thinRatio = thinRatio; }
        // Observed drawing colors. Narrow tolerance avoids merging a line into same-hue item shading.
        private static readonly Scalar[] StrokeColors =
        {
            new Scalar(0, 0, 255), new Scalar(255, 0, 0), new Scalar(0, 128, 0),
            new Scalar(0, 255, 255), new Scalar(0, 165, 255), new Scalar(128, 0, 128),
            new Scalar(203, 192, 255)
        };

        public Mat Apply(Mat colors)
        {
            using (var removal = new Mat(colors.Size(), MatType.CV_8UC1, Scalar.All(0)))
            {
                foreach (var stroke in StrokeColors) MarkSegments(colors, stroke, removal);
                if (Cv2.CountNonZero(removal) == 0) return new CaptchaColoredLineFilter().Apply(colors);
                using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)))
                    Cv2.Dilate(removal, removal, kernel);
                using (var cleaned = new Mat())
                {
                    Cv2.Inpaint(colors, removal, cleaned, 3, InpaintTypes.Telea);
                    return new CaptchaColoredLineFilter().Apply(cleaned);
                }
            }
        }

        private void MarkSegments(Mat colors, Scalar stroke, Mat removal)
        {
            using (var mask = new Mat())
            {
                Cv2.InRange(colors, new Scalar(stroke.Val0 - 8, stroke.Val1 - 8, stroke.Val2 - 8),
                    new Scalar(stroke.Val0 + 8, stroke.Val1 + 8, stroke.Val2 + 8), mask);
                foreach (var line in Cv2.HoughLinesP(mask, 1, Math.PI / 180, 12, 18, 3))
                {
                    if (!IsThin(mask, line)) continue;
                    using (var segment = new Mat(mask.Size(), MatType.CV_8UC1, Scalar.All(0)))
                    {
                        Cv2.Line(segment, line.P1, line.P2, Scalar.All(255), 7);
                        Cv2.BitwiseAnd(segment, mask, segment);
                        Cv2.BitwiseOr(removal, segment, removal);
                    }
                }
            }
        }

        private bool IsThin(Mat mask, LineSegmentPoint line)
        {
            var horizontal = line.P2.X - line.P1.X;
            var vertical = line.P2.Y - line.P1.Y;
            var length = Math.Sqrt(horizontal * horizontal + vertical * vertical);
            var inner = 0;
            var outer = 0;
            for (var row = Math.Max(0, Math.Min(line.P1.Y, line.P2.Y) - 8);
                row < Math.Min(mask.Rows, Math.Max(line.P1.Y, line.P2.Y) + 9); row++)
                for (var column = Math.Max(0, Math.Min(line.P1.X, line.P2.X) - 8);
                    column < Math.Min(mask.Cols, Math.Max(line.P1.X, line.P2.X) + 9); column++)
                {
                    if (mask.At<byte>(row, column) == 0) continue;
                    var along = ((column - line.P1.X) * horizontal + (row - line.P1.Y) * vertical) / length;
                    if (along < 0 || along > length) continue;
                    var distance = Math.Abs((column - line.P1.X) * vertical - (row - line.P1.Y) * horizontal) / length;
                    if (distance <= 3) inner++;
                    else if (distance <= 8) outer++;
                }
            return inner >= 18 && inner >= _thinRatio * outer;
        }
    }
}
