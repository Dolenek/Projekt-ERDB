using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaTemplateLibrary : IDisposable
    {
        public static readonly string[] SupportedLabels =
        {
            "apple", "banana", "chip", "coin", "dragon scale", "epic coin", "epic fish",
            "golden fish", "life potion", "mermaid hair", "normie fish", "ruby",
            "unicorn horn", "wolf skin", "zombie eye"
        };
        private readonly List<CaptchaTemplateVariant> _variants = new List<CaptchaTemplateVariant>();
        private readonly Dictionary<string, OpenCvSharp.Mat> _originals = new Dictionary<string, OpenCvSharp.Mat>();
        public IReadOnlyList<CaptchaTemplateVariant> Variants => _variants;
        public IReadOnlyList<string> Labels { get; private set; }
        public string Fingerprint { get; private set; }

        public static CaptchaTemplateLibrary Load(string directory, CaptchaItemCatalog catalog, bool includeKey = false,
            bool learned = false, bool croppedTraining = false)
        {
            var labels = includeKey ? SupportedLabels.Concat(new[] { "key" }).ToArray() : SupportedLabels;
            var catalogLabels = catalog.Items.Select(item => item.Name).ToArray();
            if (labels.Any(label => !catalogLabels.Contains(label)) ||
                catalogLabels.Any(label => !SupportedLabels.Contains(label) && label != "key"))
                throw new InvalidDataException("Local captcha catalog does not match the pipeline's supported items.");
            var library = new CaptchaTemplateLibrary { Labels = Array.AsReadOnly(labels) };
            try
            {
                library.Fingerprint = ComputeFingerprint(directory, labels);
                foreach (var label in labels)
                    library.AddVariants(directory, label);
                if (learned) library.AddLearned(directory, croppedTraining ? "Trained" : "Learned");
                return library;
            }
            catch { library.Dispose(); throw; }
        }

        public static string ComputeFingerprint(string directory, IEnumerable<string> labels = null)
        {
            using (var stream = new MemoryStream())
            {
                foreach (var label in labels ?? SupportedLabels)
                {
                    var nameBytes = Encoding.UTF8.GetBytes(label + "\n");
                    var imageBytes = File.ReadAllBytes(Path.Combine(directory, label + ".webp"));
                    stream.Write(nameBytes, 0, nameBytes.Length);
                    stream.Write(imageBytes, 0, imageBytes.Length);
                }
                using (var sha = SHA256.Create())
                    return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        private void AddVariants(string directory, string label)
        {
            var original = CaptchaTemplateTransforms.ReadTemplate(Path.Combine(directory, label + ".webp"));
            AddOriginal(label, label, original);
        }

        private void AddOriginal(string label, string sourceKey, OpenCvSharp.Mat original)
        {
            _originals.Add(sourceKey, original);
            foreach (var aspect in new[] { 0.65, 1.0, 1.5 })
                for (var angle = -30; angle <= 30; angle += 15)
                    for (var length = 12; length <= 56; length += 4)
                        using (var transformed = CaptchaTemplateTransforms.Transform(original, angle, length, aspect))
                            _variants.Add(new CaptchaTemplateVariant(label, transformed, angle, length, aspect, sourceKey));
        }

        private void AddLearned(string directory, string folder)
        {
            var learnedDirectory = Path.Combine(directory, folder);
            var paths = Directory.GetFiles(learnedDirectory, "*.webp", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (paths.Length == 0) throw new InvalidDataException("Missing learned captcha templates.");
            using (var stream = new MemoryStream())
            {
                var originalFingerprint = Encoding.UTF8.GetBytes(Fingerprint);
                stream.Write(originalFingerprint, 0, originalFingerprint.Length);
                foreach (var path in paths)
                {
                    var label = Path.GetFileName(Path.GetDirectoryName(path));
                    if (!Labels.Contains(label)) throw new InvalidDataException("Unsupported learned template label.");
                    var key = label + "/" + Path.GetFileName(path);
                    var name = Encoding.UTF8.GetBytes(key + "\n");
                    var bytes = File.ReadAllBytes(path);
                    stream.Write(name, 0, name.Length);
                    stream.Write(bytes, 0, bytes.Length);
                    AddOriginal(label, key, CaptchaTemplateTransforms.ReadTemplate(path));
                }
                using (var sha = SHA256.Create())
                    Fingerprint = BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
            }
        }

        public CaptchaTemplateVariant Refine(CaptchaTemplatePose seed, int angle, int length, double aspect)
        {
            using (var transformed = CaptchaTemplateTransforms.Transform(_originals[seed.SourceKey], angle, length, aspect))
                return new CaptchaTemplateVariant(seed.Label, transformed, angle, length, aspect, seed.SourceKey);
        }

        public void Dispose()
        {
            foreach (var variant in _variants) variant.Dispose();
            _variants.Clear();
            foreach (var original in _originals.Values) original.Dispose();
            _originals.Clear();
        }
    }
}
