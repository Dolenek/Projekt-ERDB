using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaClassifier
    {
        private readonly int _threshold;
        private readonly List<ReferenceEntry> _references;

        public CaptchaClassifier(string refsDir, int threshold)
        {
            if (string.IsNullOrWhiteSpace(refsDir))
            {
                throw new ArgumentException("Reference directory is required.", nameof(refsDir));
            }

            if (!Directory.Exists(refsDir))
            {
                throw new DirectoryNotFoundException(refsDir);
            }

            _threshold = Math.Max(1, Math.Min(64, threshold));
            _references = LoadReferences(refsDir);

            if (_references.Count == 0)
            {
                throw new InvalidOperationException("No captcha reference images were found.");
            }
        }

        public CaptchaClassificationResult Classify(byte[] imageBytes)
        {
            var ranked = Rank(imageBytes, 1);
            if (ranked.Count == 0)
            {
                return CaptchaClassificationResult.NoMatch;
            }

            var best = ranked[0];
            return new CaptchaClassificationResult(
                best.Label,
                best.Distance,
                best.Method,
                best.Distance <= _threshold);
        }

        public List<CaptchaClassificationResult> Rank(byte[] imageBytes, int count)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return new List<CaptchaClassificationResult>();
            }

            ulong targetAHash;
            ulong targetDHash;
            using (var normalized = NormalizeImage(imageBytes))
            {
                targetAHash = ComputeAverageHash(normalized);
                targetDHash = ComputeDifferenceHash(normalized);
            }

            return _references
                .Select(reference => reference.Score(targetAHash, targetDHash))
                .OrderBy(result => result.Distance)
                .ThenBy(result => result.Label, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Max(1, count))
                .ToList();
        }

        private static List<ReferenceEntry> LoadReferences(string refsDir)
        {
            var files = Directory.EnumerateFiles(refsDir, "*.*", SearchOption.TopDirectoryOnly)
                .Where(path =>
                    path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var entries = new List<ReferenceEntry>(files.Count);
            foreach (var file in files)
            {
                using (var normalized = NormalizeImage(File.ReadAllBytes(file)))
                {
                    entries.Add(new ReferenceEntry(
                        Path.GetFileNameWithoutExtension(file),
                        ComputeAverageHash(normalized),
                        ComputeDifferenceHash(normalized)));
                }
            }

            return entries;
        }

        private static Bitmap NormalizeImage(byte[] imageBytes)
        {
            using (var input = new MemoryStream(imageBytes))
            using (var source = new Bitmap(input))
            {
                var side = 64;
                var canvas = new Bitmap(side, side, PixelFormat.Format24bppRgb);

                using (var g = Graphics.FromImage(canvas))
                {
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.Black);

                    var scale = 0.9 * Math.Min(side / (double)source.Width, side / (double)source.Height);
                    var width = Math.Max(1, (int)Math.Round(source.Width * scale));
                    var height = Math.Max(1, (int)Math.Round(source.Height * scale));
                    var x = (side - width) / 2;
                    var y = (side - height) / 2;

                    using (var gray = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb))
                    using (var gg = Graphics.FromImage(gray))
                    using (var attributes = new ImageAttributes())
                    {
                        var matrix = new ColorMatrix(new[]
                        {
                            new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                            new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                            new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                            new[] { 0f, 0f, 0f, 1f, 0f },
                            new[] { 0f, 0f, 0f, 0f, 1f }
                        });

                        attributes.SetColorMatrix(matrix);
                        gg.DrawImage(source, new Rectangle(0, 0, gray.Width, gray.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                        g.DrawImage(gray, new Rectangle(x, y, width, height), new Rectangle(0, 0, gray.Width, gray.Height), GraphicsUnit.Pixel);
                    }
                }

                return canvas;
            }
        }

        private static ulong ComputeAverageHash(Bitmap source)
        {
            using (var resized = Resize(source, 8, 8))
            {
                var values = new byte[64];
                var sum = 0;
                var index = 0;

                for (var y = 0; y < 8; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        var value = resized.GetPixel(x, y).R;
                        values[index++] = value;
                        sum += value;
                    }
                }

                var average = sum / 64.0;
                ulong hash = 0;

                for (var i = 0; i < values.Length; i++)
                {
                    if (values[i] >= average)
                    {
                        hash |= 1UL << i;
                    }
                }

                return hash;
            }
        }

        private static ulong ComputeDifferenceHash(Bitmap source)
        {
            using (var resized = Resize(source, 9, 8))
            {
                ulong hash = 0;
                var bit = 0;

                for (var y = 0; y < 8; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        var left = resized.GetPixel(x, y).R;
                        var right = resized.GetPixel(x + 1, y).R;
                        if (left <= right)
                        {
                            hash |= 1UL << bit;
                        }

                        bit++;
                    }
                }

                return hash;
            }
        }

        private static Bitmap Resize(Bitmap source, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Black);
                g.DrawImage(source, new Rectangle(0, 0, width, height), new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
            }

            return bitmap;
        }

        private static int HammingDistance(ulong left, ulong right)
        {
            var value = left ^ right;
            var count = 0;
            while (value != 0)
            {
                value &= value - 1;
                count++;
            }

            return count;
        }

        private sealed class ReferenceEntry
        {
            public ReferenceEntry(string label, ulong averageHash, ulong differenceHash)
            {
                Label = label;
                AverageHash = averageHash;
                DifferenceHash = differenceHash;
            }

            public string Label { get; }
            public ulong AverageHash { get; }
            public ulong DifferenceHash { get; }

            public CaptchaClassificationResult Score(ulong targetAverageHash, ulong targetDifferenceHash)
            {
                var aHashDistance = HammingDistance(AverageHash, targetAverageHash);
                var dHashDistance = HammingDistance(DifferenceHash, targetDifferenceHash);

                if (aHashDistance <= dHashDistance)
                {
                    return new CaptchaClassificationResult(Label, aHashDistance, "ahash", false);
                }

                return new CaptchaClassificationResult(Label, dHashDistance, "dhash", false);
            }
        }
    }

    public sealed class CaptchaClassificationResult
    {
        public static readonly CaptchaClassificationResult NoMatch =
            new CaptchaClassificationResult(string.Empty, int.MaxValue, "none", false);

        public CaptchaClassificationResult(string label, int distance, string method, bool isMatch)
        {
            Label = label ?? string.Empty;
            Distance = distance;
            Method = method ?? string.Empty;
            IsMatch = isMatch;
        }

        public string Label { get; }
        public int Distance { get; }
        public string Method { get; }
        public bool IsMatch { get; }
    }
}
