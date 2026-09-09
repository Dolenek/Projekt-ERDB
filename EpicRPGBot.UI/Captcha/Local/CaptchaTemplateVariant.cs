using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaTemplateVariant : IDisposable
    {
        private readonly List<ColorReference> _colorReferences = new List<ColorReference>();
        public CaptchaTemplateVariant(string label, Mat colors, int angle = 0, int length = 0, double aspect = 1,
            string sourceKey = null)
        {
            Label = label;
            SourceKey = sourceKey ?? label;
            Angle = angle;
            Length = length;
            Aspect = aspect;
            Gray = new Mat();
            Cv2.CvtColor(colors, Gray, ColorConversionCodes.BGR2GRAY);
            MaximumGray = CaptchaLuminance.MaximumChannel(colors);
            Binary = CaptchaForeground.CreateMask(colors);
            ForegroundArea = Cv2.Sum(Binary).Val0;
            var rows = colors.Rows; var columns = colors.Cols;
            for (var y = 0; y < rows; y++)
                for (var x = 0; x < columns; x++)
                    AddReference(colors.At<Vec3b>(y, x), x, y);
        }
        public string Label { get; }
        public string SourceKey { get; }
        public int Angle { get; }
        public int Length { get; }
        public double Aspect { get; }
        public Mat Gray { get; }
        public Mat MaximumGray { get; }
        public Mat Binary { get; }
        public double ForegroundArea { get; }

        public double ColorSimilarity(Mat scene, Point location)
        {
            if (_colorReferences.Count <= 5) return 1;
            double dot = 0, actualNorm = 0, referenceNorm = 0, chroma = 0;
            foreach (var reference in _colorReferences)
            {
                var pixel = scene.At<Vec3b>(location.Y + reference.Y, location.X + reference.X);
                var mean = (pixel.Item0 + pixel.Item1 + pixel.Item2) / 3.0;
                var blue = pixel.Item0 - mean;
                var green = pixel.Item1 - mean;
                var red = pixel.Item2 - mean;
                dot += blue * reference.Blue + green * reference.Green + red * reference.Red;
                actualNorm += blue * blue + green * green + red * red;
                referenceNorm += reference.Blue * reference.Blue + reference.Green * reference.Green + reference.Red * reference.Red;
                chroma += Math.Max(pixel.Item0, Math.Max(pixel.Item1, pixel.Item2)) -
                    Math.Min(pixel.Item0, Math.Min(pixel.Item1, pixel.Item2));
            }
            if (chroma / _colorReferences.Count <= 18) return 1;
            return Math.Max(0, Math.Min(1, dot / (Math.Sqrt(actualNorm * referenceNorm) + 1e-8)));
        }

        private void AddReference(Vec3b pixel, int x, int y)
        {
            var maximum = Math.Max(pixel.Item0, Math.Max(pixel.Item1, pixel.Item2));
            var minimum = Math.Min(pixel.Item0, Math.Min(pixel.Item1, pixel.Item2));
            if (maximum <= 65 || maximum - minimum <= 25) return;
            var mean = (pixel.Item0 + pixel.Item1 + pixel.Item2) / 3.0;
            _colorReferences.Add(new ColorReference
            {
                X = x, Y = y, Blue = pixel.Item0 - mean,
                Green = pixel.Item1 - mean, Red = pixel.Item2 - mean
            });
        }

        public void Dispose() { Gray.Dispose(); MaximumGray.Dispose(); Binary.Dispose(); }

        private sealed class ColorReference
        {
            public int X;
            public int Y;
            public double Blue;
            public double Green;
            public double Red;
        }
    }
}
