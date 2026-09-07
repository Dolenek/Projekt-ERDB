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
        public IReadOnlyList<CaptchaTemplateVariant> Variants => _variants;
        public string Fingerprint { get; private set; }

        public static CaptchaTemplateLibrary Load(string directory, CaptchaItemCatalog catalog)
        {
            if (!catalog.Items.Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(SupportedLabels.OrderBy(name => name, StringComparer.Ordinal)))
                throw new InvalidDataException("Local captcha catalog must contain the 15 supported items.");
            var library = new CaptchaTemplateLibrary();
            try
            {
                library.Fingerprint = ComputeFingerprint(directory);
                foreach (var label in SupportedLabels)
                    library.AddVariants(directory, label);
                return library;
            }
            catch { library.Dispose(); throw; }
        }

        public static string ComputeFingerprint(string directory)
        {
            using (var stream = new MemoryStream())
            {
                foreach (var label in SupportedLabels)
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
            using (var original = CaptchaTemplateTransforms.ReadTemplate(Path.Combine(directory, label + ".webp")))
                foreach (var aspect in new[] { 0.65, 1.0, 1.5 })
                for (var angle = -30; angle <= 30; angle += 15)
                    for (var length = 12; length <= 56; length += 4)
                        using (var transformed = CaptchaTemplateTransforms.Transform(original, angle, length, aspect))
                            _variants.Add(new CaptchaTemplateVariant(label, transformed));
        }

        public void Dispose()
        {
            foreach (var variant in _variants) variant.Dispose();
            _variants.Clear();
        }
    }
}
