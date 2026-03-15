using System;
using System.Collections.Generic;
using System.IO;

namespace EpicRPGBot.UI.Services
{
    public sealed class LocalSettingsStore
    {
        private readonly string _filePath;

        public LocalSettingsStore(string fileName = "app-settings.ini")
        {
            _filePath = Path.Combine(GetSettingsRoot(), fileName);
        }

        public string GetString(string key, string defaultValue = null)
        {
            var values = ReadAll();
            return values.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public bool GetBool(string key, bool defaultValue)
        {
            var raw = GetString(key, null);
            return bool.TryParse(raw, out var value) ? value : defaultValue;
        }

        public void SetString(string key, string value)
        {
            var values = ReadAll();
            values[key] = value ?? string.Empty;
            WriteAll(values);
        }

        public void SetBool(string key, bool value)
        {
            SetString(key, value ? "true" : "false");
        }

        private Dictionary<string, string> ReadAll()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!File.Exists(_filePath))
                {
                    return values;
                }

                foreach (var line in File.ReadAllLines(_filePath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var index = trimmed.IndexOf('=');
                    if (index <= 0)
                    {
                        continue;
                    }

                    var key = trimmed.Substring(0, index).Trim();
                    var value = trimmed.Substring(index + 1).Trim();
                    values[key] = value;
                }
            }
            catch
            {
            }

            return values;
        }

        private void WriteAll(Dictionary<string, string> values)
        {
            try
            {
                Directory.CreateDirectory(GetSettingsRoot());

                var lines = new List<string>();
                foreach (var pair in values)
                {
                    lines.Add($"{pair.Key}={pair.Value}");
                }

                File.WriteAllLines(_filePath, lines);
            }
            catch
            {
            }
        }

        private static string GetSettingsRoot()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EpicRPGBot.UI",
                "settings");
        }
    }
}
