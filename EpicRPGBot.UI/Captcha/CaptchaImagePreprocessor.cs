using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace EpicRPGBot.UI.Captcha
{
    public static class CaptchaImagePreprocessor
    {
        public static byte[] CreateRetryImage(byte[] sourceImageBytes)
        {
            if (sourceImageBytes == null || sourceImageBytes.Length == 0)
            {
                return null;
            }

            try
            {
                using (var input = new MemoryStream(sourceImageBytes))
                using (var source = new Bitmap(input))
                {
                    var scale = Math.Max(2, Math.Min(4, 256 / Math.Max(1, Math.Max(source.Width, source.Height))));
                    var width = Math.Max(source.Width, source.Width * scale);
                    var height = Math.Max(source.Height, source.Height * scale);

                    using (var canvas = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                    using (var graphics = Graphics.FromImage(canvas))
                    using (var output = new MemoryStream())
                    {
                        graphics.Clear(Color.FromArgb(18, 18, 18));
                        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                        graphics.PixelOffsetMode = PixelOffsetMode.Half;
                        graphics.SmoothingMode = SmoothingMode.None;
                        graphics.DrawImage(source, new Rectangle(0, 0, width, height));
                        canvas.Save(output, ImageFormat.Png);
                        return output.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
