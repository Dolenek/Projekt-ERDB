using System;
using System.IO;

namespace EpicRPGBot.UI
{
    public static class Env
    {
        public static void Load()
        {
            try
            {
                var path = FindFileUpwards(".env");
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return;
                }

                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var index = line.IndexOf('=');
                    if (index <= 0)
                    {
                        continue;
                    }

                    var key = line.Substring(0, index).Trim();
                    if (!key.StartsWith("CAPTCHA_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var value = line.Substring(index + 1).Trim();
                    if ((value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal)) ||
                        (value.StartsWith("'", StringComparison.Ordinal) && value.EndsWith("'", StringComparison.Ordinal)))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch
            {
            }
        }

        public static string Get(string key, string fallback = null)
        {
            try
            {
                var value = Environment.GetEnvironmentVariable(key);
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }

        private static string FindFileUpwards(string fileName)
        {
            try
            {
                var current = AppDomain.CurrentDomain.BaseDirectory;
                for (var i = 0; i < 8; i++)
                {
                    var candidate = Path.Combine(current, fileName);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    var parent = Directory.GetParent(current);
                    if (parent == null)
                    {
                        break;
                    }

                    current = parent.FullName;
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
