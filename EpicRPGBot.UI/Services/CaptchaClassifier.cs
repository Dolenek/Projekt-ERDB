using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace EpicRPGBot.UI.Services
{
    // Lightweight perceptual-hash based classifier for 16 known reference images.
    // - Loads references from a directory (default: EpicRPGBot.UI/CaptchaRefs).
    // - Computes and caches both dHash (64-bit) and pHash (64-bit) for robustness.
    // - Classifies an incoming image by minimal Hamming distance under both hashes.
    //
    // Net48 compatibility:
    // - Avoids Span<T> and unsafe code. Uses simple managed loops (images are small).
    //
    // Usage:
    //   var clf = new CaptchaClassifier(refsDir, thresholdBits: 12);
    //   var result = clf.Classify(bytes);
    //   if (result.IsMatch) Send(result.Label);
    public sealed class CaptchaClassifier
    {
        public sealed class ClassifyResult
        {
            public string Label { get; set; }
            public int Distance { get; set; }
            public string Method { get; set; } // "dhash" or "phash"
            public bool IsMatch { get; set; }
        }

        private readonly string _refsDir;
        private readonly int _thresholdBits;
        private readonly Dictionary<string, ulong> _refDHash = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ulong> _refPHash = new Dictionary<string, ulong>(StringComparer.OrdinalIgnoreCase);

        public CaptchaClassifier(string refsDir, int thresholdBits = 12)
        {
            if (string.IsNullOrWhiteSpace(refsDir))
                throw new ArgumentNullException(nameof(refsDir));

            _refsDir = refsDir;
            _thresholdBits = thresholdBits <= 0 ? 12 : thresholdBits;
            LoadReferences();
        }

        private void LoadReferences()
        {
            if (!Directory.Exists(_refsDir))
                throw new DirectoryNotFoundException($"Captcha references directory not found: {_refsDir}");

            var files = Directory.EnumerateFiles(_refsDir, "*.png", SearchOption.TopDirectoryOnly)
                                 .Concat(Directory.EnumerateFiles(_refsDir, "*.jpg", SearchOption.TopDirectoryOnly))
                                 .Concat(Directory.EnumerateFiles(_refsDir, "*.jpeg", SearchOption.TopDirectoryOnly))
                                 .ToList();

            if (files.Count == 0)
                throw new InvalidOperationException($"No reference images found in: {_refsDir}");

            int loaded = 0;
            foreach (var f in files)
            {
                try
                {
                    var label = Path.GetFileNameWithoutExtension(f); // exact answer text per user
                    using (var bmp = new Bitmap(f))
                    using (var pre = PreprocessForHash(bmp))
                    {
                        var dh = ImageHash.ComputeDHash64(pre);
                        var ph = ImageHash.ComputePHash64(pre);
                        _refDHash[label] = dh;
                        _refPHash[label] = ph;
                        loaded++;
                    }
                }
                catch
                {
                    // Skip invalid/unsupported/corrupted image and continue
                    // Common cause: WEBP files renamed to .png/.jpg. Convert to real PNG/JPG.
                }
            }

            if (loaded == 0)
                throw new InvalidOperationException($"No valid reference images could be loaded from: {_refsDir}. Ensure files are real PNG/JPG (not WEBP) and not corrupted.");
        }

        // Main entry: Classify image bytes to best matching label (within threshold).
        public ClassifyResult Classify(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return new ClassifyResult { Label = null, Distance = int.MaxValue, Method = "", IsMatch = false };

            try
            {
                using (var ms = new MemoryStream(bytes))
                using (var bmp = new Bitmap(ms))
                using (var pre = PreprocessForHash(bmp))
                {
                    var dh = ImageHash.ComputeDHash64(pre);
                    var ph = ImageHash.ComputePHash64(pre);

                    string bestLabel = null;
                    int bestDist = int.MaxValue;
                    string bestMethod = "dhash";

                    foreach (var kv in _refDHash)
                    {
                        var dist = ImageHash.HammingDistance64(dh, kv.Value);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestLabel = kv.Key;
                            bestMethod = "dhash";
                        }
                    }

                    foreach (var kv in _refPHash)
                    {
                        var dist = ImageHash.HammingDistance64(ph, kv.Value);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestLabel = kv.Key;
                            bestMethod = "phash";
                        }
                    }

                    return new ClassifyResult
                    {
                        Label = bestLabel,
                        Distance = bestDist,
                        Method = bestMethod,
                        IsMatch = bestDist <= _thresholdBits
                    };
                }
            }
            catch
            {
                // Invalid/unsupported image bytes (e.g., WEBP). Treat as no match.
                return new ClassifyResult { Label = null, Distance = int.MaxValue, Method = "", IsMatch = false };
            }
        }

        // Return top-N closest matches (by min distance across dHash/pHash), for debugging/self-test.
        public List<ClassifyResult> Rank(byte[] bytes, int topN = 5)
        {
            var results = new List<ClassifyResult>();
            if (bytes == null || bytes.Length == 0) return results;

            try
            {
                using (var ms = new MemoryStream(bytes))
                using (var bmp = new Bitmap(ms))
                using (var pre = PreprocessForHash(bmp))
                {
                    var dh = ImageHash.ComputeDHash64(pre);
                    var ph = ImageHash.ComputePHash64(pre);

                    foreach (var kv in _refDHash)
                    {
                        var label = kv.Key;
                        var rd = kv.Value;
                        _refPHash.TryGetValue(label, out var rp);

                        int distD = ImageHash.HammingDistance64(dh, rd);
                        int distP = ImageHash.HammingDistance64(ph, rp);
                        bool useD = distD <= distP;
                        int best = useD ? distD : distP;
                        string method = useD ? "dhash" : "phash";

                        results.Add(new ClassifyResult
                        {
                            Label = label,
                            Distance = best,
                            Method = method,
                            IsMatch = best <= _thresholdBits
                        });
                    }
                }

                return results
                    .OrderBy(r => r.Distance)
                    .ThenBy(r => r.Label, StringComparer.OrdinalIgnoreCase)
                    .Take(Math.Max(1, topN))
                    .ToList();
            }
            catch
            {
                return new List<ClassifyResult>();
            }
        }

        // Normalize: letterbox to square, grayscale, optional median filter to soften thin overlays.
        private static Bitmap PreprocessForHash(Bitmap src)
        {
            // Convert to square canvas with letterboxing to preserve aspect
            int target = 64; // enough for pHash pre-scale; dHash uses its own downsizing
            var canvas = new Bitmap(target, target, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Black);

                double sw = src.Width;
                double sh = src.Height;
                double scale = Math.Min(target / sw, target / sh);
                int w = Math.Max(1, (int)Math.Round(sw * scale));
                int h = Math.Max(1, (int)Math.Round(sh * scale));
                int x = (target - w) / 2;
                int y = (target - h) / 2;

                using (var gray = ToGrayscale(src))
                {
                    g.DrawImage(gray, new Rectangle(x, y, w, h), new Rectangle(0, 0, gray.Width, gray.Height), GraphicsUnit.Pixel);
                }
            }

            // Apply a light 3x3 median filter to reduce thin noise lines (canvas is small: 64x64)
            var filtered = MedianFilter3x3_NoUnsafe(canvas);
            canvas.Dispose();
            return filtered;
        }

        private static Bitmap ToGrayscale(Bitmap src)
        {
            var dst = new Bitmap(src.Width, src.Height, PixelFormat.Format24bppRgb);

            // Luminosity weights
            const float rw = 0.299f;
            const float gw = 0.587f;
            const float bw = 0.114f;

            var cm = new ColorMatrix(new float[][]
            {
                new float[] { rw, rw, rw, 0, 0},
                new float[] { gw, gw, gw, 0, 0},
                new float[] { bw, bw, bw, 0, 0},
                new float[] {  0,  0,  0, 1, 0},
                new float[] {  0,  0,  0, 0, 1}
            });

            using (var ia = new ImageAttributes())
            {
                ia.SetColorMatrix(cm);
                using (var g = Graphics.FromImage(dst))
                {
                    g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
                        0, 0, src.Width, src.Height, GraphicsUnit.Pixel, ia);
                }
            }
            return dst;
        }

        // Median filter without unsafe/Span; fine for 64x64.
        private static Bitmap MedianFilter3x3_NoUnsafe(Bitmap src)
        {
            int w = src.Width, h = src.Height;
            var dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    {
                        dst.SetPixel(x, y, src.GetPixel(x, y));
                        continue;
                    }

                    byte[] vals = new byte[9];
                    int k = 0;
                    for (int yy = -1; yy <= 1; yy++)
                    {
                        for (int xx = -1; xx <= 1; xx++)
                        {
                            var c = src.GetPixel(x + xx, y + yy);
                            // grayscale image; any channel
                            vals[k++] = c.B;
                        }
                    }
                    Array.Sort(vals);
                    byte median = vals[4];
                    dst.SetPixel(x, y, Color.FromArgb(median, median, median));
                }
            }
            return dst;
        }
    }

    internal static class ImageHash
    {
        // 64-bit dHash: compute 9x8 grayscale thumbnail, compare adjacent pixels horizontally.
        public static ulong ComputeDHash64(Bitmap src)
        {
            using (var thumb = Resize(src, 9, 8))
            {
                ulong hash = 0UL;
                int bit = 0;

                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        var p1 = GetGray(thumb, x, y);
                        var p2 = GetGray(thumb, x + 1, y);
                        if (p1 > p2) hash |= 1UL << bit;
                        bit++;
                    }
                }
                return hash;
            }
        }

        // 64-bit pHash: DCT 32x32 -> take top-left 8x8 (excluding DC) median threshold.
        public static ulong ComputePHash64(Bitmap src)
        {
            using (var img = Resize(src, 32, 32))
            {
                double[,] g = ToGrayMatrix(img);
                var dct = DCT2D(g);

                double[] vals = new double[64];
                int k = 0;
                for (int y = 0; y < 8; y++)
                    for (int x = 0; x < 8; x++)
                        vals[k++] = dct[y, x];

                double median = Median(vals);

                ulong hash = 0;
                for (int i = 0; i < 64; i++)
                {
                    if (i == 0) continue; // skip DC
                    if (vals[i] > median) hash |= 1UL << (i - 1);
                }
                return hash;
            }
        }

        public static int HammingDistance64(ulong a, ulong b)
        {
            ulong x = a ^ b;
            int c = 0;
            while (x != 0)
            {
                x &= x - 1;
                c++;
            }
            return c;
        }

        private static Bitmap Resize(Bitmap src, int w, int h)
        {
            var dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, 0, 0, w, h);
            }
            return dst;
        }

        private static byte GetGray(Bitmap bmp, int x, int y)
        {
            var c = bmp.GetPixel(x, y);
            return c.B; // grayscale already
        }

        private static double[,] ToGrayMatrix(Bitmap bmp)
        {
            int w = bmp.Width;
            int h = bmp.Height;
            var m = new double[h, w];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    m[y, x] = c.B / 255.0;
                }
            }
            return m;
        }

        private static double[,] DCT2D(double[,] input)
        {
            int h = input.GetLength(0);
            int w = input.GetLength(1);
            var output = new double[h, w];

            double[] cosX = new double[w * w];
            double[] cosY = new double[h * h];

            for (int u = 0; u < w; u++)
                for (int x = 0; x < w; x++)
                    cosX[u * w + x] = Math.Cos(((2 * x + 1) * u * Math.PI) / (2 * w));

            for (int v = 0; v < h; v++)
                for (int y = 0; y < h; y++)
                    cosY[v * h + y] = Math.Cos(((2 * y + 1) * v * Math.PI) / (2 * h));

            for (int v = 0; v < h; v++)
            {
                for (int u = 0; u < w; u++)
                {
                    double sum = 0.0;
                    for (int y = 0; y < h; y++)
                    {
                        double cy = cosY[v * h + y];
                        for (int x = 0; x < w; x++)
                        {
                            sum += input[y, x] * cosX[u * w + x] * cy;
                        }
                    }

                    double cu = u == 0 ? 1 / Math.Sqrt(2) : 1;
                    double cv = v == 0 ? 1 / Math.Sqrt(2) : 1;
                    output[v, u] = 2.0 / Math.Sqrt(w * h) * cu * cv * sum;
                }
            }
            return output;
        }

        private static double Median(double[] arr)
        {
            if (arr == null || arr.Length == 0) return 0;
            var copy = (double[])arr.Clone();
            Array.Sort(copy);
            int n = copy.Length;
            if (n % 2 == 1) return copy[n / 2];
            return 0.5 * (copy[n / 2 - 1] + copy[n / 2]);
        }
    }
}