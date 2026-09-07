using System;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal static class CaptchaForeground
    {
        public static Mat CreateMask(Mat colors)
        {
            var rows = colors.Rows; var columns = colors.Cols;
            var mask = new Mat(rows, columns, MatType.CV_32FC1);
            for (var y = 0; y < rows; y++)
                for (var x = 0; x < columns; x++)
                {
                    var pixel = colors.At<Vec3b>(y, x);
                    var difference = Math.Max(Math.Abs(pixel.Item0 - 34),
                        Math.Max(Math.Abs(pixel.Item1 - 34), Math.Abs(pixel.Item2 - 34)));
                    mask.Set(y, x, difference > 10 ? 1f : 0f);
                }
            return mask;
        }
    }
}
