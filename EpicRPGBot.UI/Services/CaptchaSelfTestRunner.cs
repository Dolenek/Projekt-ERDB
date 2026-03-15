using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaSelfTestRunner
    {
        public async Task RunAsync(Action<string> logInfo)
        {
            await Task.Yield();

            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var defaultRefs = Path.Combine(baseDir, "CaptchaRefs");
                var refsDir = Env.Get("CAPTCHA_REFS_DIR", defaultRefs);
                var threshold = ReadThreshold();

                if (!Directory.Exists(refsDir))
                {
                    logInfo?.Invoke($"[selftest] Refs dir not found: {refsDir}");
                    return;
                }

                var files = EnumerateReferenceFiles(refsDir);
                logInfo?.Invoke($"[selftest] Found {files.Count} refs in {refsDir}. Threshold={threshold}");

                var classifier = new CaptchaClassifier(refsDir, threshold);
                foreach (var file in files)
                {
                    await RunCaseAsync(classifier, file, logInfo);
                }

                logInfo?.Invoke("[selftest] Completed.");
            }
            catch (Exception ex)
            {
                logInfo?.Invoke("[selftest] Error: " + ex.Message);
            }
        }

        private static async Task RunCaseAsync(CaptchaClassifier classifier, string file, Action<string> logInfo)
        {
            await Task.Yield();

            var label = Path.GetFileNameWithoutExtension(file);
            var original = File.ReadAllBytes(file);
            var originalResult = classifier.Classify(original);
            logInfo?.Invoke($"[selftest] {label}: original => {originalResult.Label} (dist={originalResult.Distance}, method={originalResult.Method})");

            var topMatches = classifier.Rank(original, 3);
            if (topMatches.Count > 0)
            {
                var line = string.Join("; ", topMatches.Select((match, index) => $"{index + 1}) {match.Label} (d={match.Distance}, {match.Method})"));
                logInfo?.Invoke($"[selftest] {label}: original top -> {line}");
            }

            using (var bitmap = new Bitmap(file))
            using (var variant = CreateVariant(bitmap))
            using (var stream = new MemoryStream())
            {
                variant.Save(stream, ImageFormat.Png);
                var variantResult = classifier.Classify(stream.ToArray());
                logInfo?.Invoke($"[selftest] {label}: variant => {variantResult.Label} (dist={variantResult.Distance}, method={variantResult.Method})");
            }
        }

        private static List<string> EnumerateReferenceFiles(string refsDir)
        {
            return Directory.EnumerateFiles(refsDir, "*.png", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(refsDir, "*.jpg", SearchOption.TopDirectoryOnly))
                .Concat(Directory.EnumerateFiles(refsDir, "*.jpeg", SearchOption.TopDirectoryOnly))
                .ToList();
        }

        private static int ReadThreshold()
        {
            var threshold = 12;

            try
            {
                var raw = Env.Get("CAPTCHA_HASH_THRESHOLD", null);
                if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var parsed) && parsed > 0 && parsed <= 64)
                {
                    threshold = parsed;
                }
            }
            catch
            {
            }

            return threshold;
        }

        private static Bitmap CreateVariant(Bitmap source)
        {
            var target = Math.Max(64, Math.Min(128, Math.Max(source.Width, source.Height)));
            var canvas = new Bitmap(target, target, PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(canvas))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.Clear(Color.Black);

                var scale = 0.9 * Math.Min(target / (double)source.Width, target / (double)source.Height);
                var width = Math.Max(1, (int)Math.Round(source.Width * scale));
                var height = Math.Max(1, (int)Math.Round(source.Height * scale));
                var x = (target - width) / 2;
                var y = (target - height) / 2;

                using (var gray = new Bitmap(source.Width, source.Height, PixelFormat.Format24bppRgb))
                using (var gg = Graphics.FromImage(gray))
                {
                    var matrix = new ColorMatrix(new[]
                    {
                        new[] { 0.299f, 0.299f, 0.299f, 0f, 0f },
                        new[] { 0.587f, 0.587f, 0.587f, 0f, 0f },
                        new[] { 0.114f, 0.114f, 0.114f, 0f, 0f },
                        new[] { 0f, 0f, 0f, 1f, 0f },
                        new[] { 0f, 0f, 0f, 0f, 1f }
                    });

                    using (var attributes = new ImageAttributes())
                    {
                        attributes.SetColorMatrix(matrix);
                        gg.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                    }

                    g.DrawImage(gray, new Rectangle(x, y, width, height), new Rectangle(0, 0, gray.Width, gray.Height), GraphicsUnit.Pixel);
                }

                using (var pen = new Pen(Color.FromArgb(255, 255, 255), 1))
                {
                    for (var i = 1; i <= 4; i++)
                    {
                        var yy = i * (target / 5);
                        g.DrawLine(pen, 0, yy, target - 1, yy);
                    }
                }
            }

            return canvas;
        }
    }
}
