using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace CaptchaReplay
{
    internal static class ReplayDataset
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        public static List<ReplayRecord> Read(string manifest)
        {
            var records = JsonSerializer.Deserialize<List<ReplayRecord>>(File.ReadAllText(manifest), JsonOptions);
            if (records == null || records.Count == 0) throw new InvalidDataException("Empty replay manifest.");
            if (records.Any(record => record == null || string.IsNullOrWhiteSpace(record.Expected) ||
                string.IsNullOrWhiteSpace(record.Image) || record.Sha256 == null || record.Sha256.Length != 64 ||
                record.Sha256.Any(character => !Uri.IsHexDigit(character))))
                throw new InvalidDataException("Replay records require a label, image path and SHA-256.");
            if (records.Select(record => record.Sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count() != records.Count)
                throw new InvalidDataException("Duplicate images in replay manifest.");
            return records;
        }

        public static byte[] ReadImage(string directory, ReplayRecord record)
        {
            var bytes = File.ReadAllBytes(Path.Combine(directory, record.Image));
            using (var hash = SHA256.Create())
            {
                var actual = BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                if (!string.Equals(actual, record.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Image hash mismatch: " + record.Image);
            }
            return bytes;
        }

        public static void EnsureHoldoutSeparation(string manifest, List<ReplayRecord> records)
        {
            if (Path.GetFileName(manifest) != "holdout.json") throw new InvalidOperationException("Validation requires holdout.json.");
            var calibration = Read(Path.Combine(Path.GetDirectoryName(manifest), "calibration.json"));
            var hashes = new HashSet<string>(calibration.Select(record => record.Sha256), StringComparer.OrdinalIgnoreCase);
            if (records.Any(record => hashes.Contains(record.Sha256)))
                throw new InvalidDataException("Calibration/holdout overlap.");
        }
    }
}
