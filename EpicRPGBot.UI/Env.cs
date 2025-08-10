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
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    int idx = line.IndexOf('=');
                    if (idx <= 0) continue;

                    string key = line.Substring(0, idx).Trim();
                    string value = line.Substring(idx + 1).Trim();
                    if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
            catch { }
        }

        public static string Get(string key, string fallback = null)
        {
            try
            {
                var v = Environment.GetEnvironmentVariable(key);
                return string.IsNullOrWhiteSpace(v) ? fallback : v;
            }
            catch { return fallback; }
        }

        private static string FindFileUpwards(string fileName)
        {
            try
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 8; i++)
                {
                    string candidate = Path.Combine(current, fileName);
                    if (File.Exists(candidate)) return candidate;
                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }
            }
            catch { }
            return null;
        }
    }
}