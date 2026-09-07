using System;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal static class CaptchaTemplateTransforms
    {
        public static Mat ReadTemplate(string path)
        {
            using (var source = Cv2.ImRead(path, ImreadModes.Unchanged))
            {
                if (source.Empty()) throw new InvalidOperationException("Cannot decode template: " + path);
                var colors = new Mat(source.Rows, source.Cols, MatType.CV_8UC3, Scalar.All(34));
                var rows = source.Rows; var columns = source.Cols;
                for (var y = 0; y < rows; y++)
                    for (var x = 0; x < columns; x++)
                        colors.Set(y, x, Composite(source, x, y));
                using (colors)
                    return CropForeground(colors, 0);
            }
        }

        public static Mat Transform(Mat original, int angle, int length, double aspect)
        {
            using (var stretched = new Mat())
            {
                Cv2.Resize(original, stretched, new Size(), aspect, 1, InterpolationFlags.Linear);
                return TransformScaled(stretched, angle, length);
            }
        }

        private static Mat TransformScaled(Mat original, int angle, int length)
        {
            var scale = (double)length / Math.Max(original.Rows, original.Cols);
            using (var scaled = new Mat())
            {
                Cv2.Resize(original, scaled, new Size(), scale, scale, InterpolationFlags.Linear);
                var side = (int)Math.Ceiling(Math.Max(scaled.Rows, scaled.Cols) * 1.6) + 8;
                using (var canvas = new Mat(side, side, MatType.CV_8UC3, Scalar.All(34)))
                using (var destination = new Mat(canvas, new Rect((side - scaled.Cols) / 2,
                    (side - scaled.Rows) / 2, scaled.Cols, scaled.Rows)))
                using (var rotation = Cv2.GetRotationMatrix2D(new Point2f(side / 2f, side / 2f), angle, 1))
                using (var rotated = new Mat())
                {
                    scaled.CopyTo(destination);
                    Cv2.WarpAffine(canvas, rotated, rotation, new Size(side, side),
                        InterpolationFlags.Linear, BorderTypes.Constant, Scalar.All(34));
                    return CropForeground(rotated, 3);
                }
            }
        }

        private static Vec3b Composite(Mat source, int x, int y)
        {
            if (source.Channels() == 3) return source.At<Vec3b>(y, x);
            if (source.Channels() != 4) throw new InvalidOperationException("Template must be RGB or RGBA.");
            var pixel = source.At<Vec4b>(y, x);
            var alpha = pixel.Item3 / 255.0;
            return new Vec3b((byte)(pixel.Item0 * alpha + 34 * (1 - alpha)),
                (byte)(pixel.Item1 * alpha + 34 * (1 - alpha)),
                (byte)(pixel.Item2 * alpha + 34 * (1 - alpha)));
        }

        private static Mat CropForeground(Mat colors, int padding)
        {
            var left = colors.Cols; var right = -1; var top = colors.Rows; var bottom = -1;
            var rows = colors.Rows; var columns = colors.Cols;
            for (var y = 0; y < rows; y++)
                for (var x = 0; x < columns; x++)
                {
                    var pixel = colors.At<Vec3b>(y, x);
                    if (Math.Max(Math.Abs(pixel.Item0 - 34),
                        Math.Max(Math.Abs(pixel.Item1 - 34), Math.Abs(pixel.Item2 - 34))) <= 8) continue;
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                }
            if (right < 0) throw new InvalidOperationException("Empty captcha template.");
            left = Math.Max(0, left - padding); top = Math.Max(0, top - padding);
            right = Math.Min(colors.Cols - 1, right + padding); bottom = Math.Min(colors.Rows - 1, bottom + padding);
            using (var region = new Mat(colors, new Rect(left, top, right - left + 1, bottom - top + 1)))
                return region.Clone();
        }
    }
}
