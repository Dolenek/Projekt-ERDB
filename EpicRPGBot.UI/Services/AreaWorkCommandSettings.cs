using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.Services
{
    public static class AreaWorkCommandSettings
    {
        public const int MinimumArea = 1;
        public const int MaximumArea = 15;

        public static string DefaultSerializedMap => Serialize(CreateDefaultSelections());

        public static Dictionary<int, string> CreateDefaultSelections()
        {
            var selections = new Dictionary<int, string>();
            for (var area = MinimumArea; area <= MaximumArea; area++)
            {
                selections[area] = GetDefaultCommandText(area);
            }

            return selections;
        }

        public static Dictionary<int, string> Parse(string serializedSelections)
        {
            var selections = CreateDefaultSelections();
            if (string.IsNullOrWhiteSpace(serializedSelections))
            {
                return selections;
            }

            var pairs = SplitEscaped(serializedSelections, ';');
            foreach (var pair in pairs)
            {
                var parts = SplitPair(pair);
                if (parts.Length != 2 || !int.TryParse(parts[0].Trim(), out var area))
                {
                    continue;
                }

                area = NormalizeArea(area);
                var commandText = NormalizeCommandText(Unescape(parts[1]), area);
                if (string.IsNullOrWhiteSpace(commandText))
                {
                    continue;
                }

                selections[area] = commandText;
            }

            return selections;
        }

        public static string Serialize(IReadOnlyDictionary<int, string> selections)
        {
            var parts = new List<string>();
            for (var area = MinimumArea; area <= MaximumArea; area++)
            {
                var commandText = GetDefaultCommandText(area);
                if (selections != null && selections.TryGetValue(area, out var configuredCommandText))
                {
                    var normalizedCommandText = NormalizeCommandText(configuredCommandText, area);
                    if (!string.IsNullOrWhiteSpace(normalizedCommandText))
                    {
                        commandText = normalizedCommandText;
                    }
                }

                parts.Add($"{area}={Escape(commandText)}");
            }

            return string.Join(";", parts);
        }

        public static string ResolveCommandText(string serializedSelections, int area)
        {
            var selections = Parse(serializedSelections);
            return selections[NormalizeArea(area)];
        }

        public static int NormalizeArea(int area)
        {
            if (area < MinimumArea)
            {
                return MinimumArea;
            }

            return area > MaximumArea ? MaximumArea : area;
        }

        public static string NormalizeCommandText(string commandText, int area)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                return GetDefaultCommandText(area);
            }

            var trimmed = commandText.Trim();
            return trimmed.StartsWith("rpg ", StringComparison.OrdinalIgnoreCase)
                ? trimmed
                : $"rpg {trimmed}";
        }

        private static string GetDefaultCommandText(int area)
        {
            if (area <= 2) return "rpg chop";
            if (area <= 5) return "rpg axe";
            if (area <= 8) return "rpg bowsaw";
            return "rpg chainsaw";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace(";", "\\;")
                .Replace("=", "\\=");
        }

        private static string Unescape(string value)
        {
            var chars = new List<char>();
            var escaping = false;
            foreach (var ch in value ?? string.Empty)
            {
                if (escaping)
                {
                    chars.Add(ch);
                    escaping = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaping = true;
                    continue;
                }

                chars.Add(ch);
            }

            if (escaping)
            {
                chars.Add('\\');
            }

            return new string(chars.ToArray());
        }

        private static string[] SplitEscaped(string value, char separator)
        {
            var parts = new List<string>();
            var current = new List<char>();
            var escaping = false;

            foreach (var ch in value ?? string.Empty)
            {
                if (escaping)
                {
                    current.Add(ch);
                    escaping = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (ch == separator)
                {
                    parts.Add(new string(current.ToArray()));
                    current.Clear();
                    continue;
                }

                current.Add(ch);
            }

            if (escaping)
            {
                current.Add('\\');
            }

            parts.Add(new string(current.ToArray()));
            return parts.ToArray();
        }

        private static string[] SplitPair(string pair)
        {
            var escaping = false;
            for (var index = 0; index < (pair ?? string.Empty).Length; index++)
            {
                var ch = pair[index];
                if (escaping)
                {
                    escaping = false;
                    continue;
                }

                if (ch == '\\')
                {
                    escaping = true;
                    continue;
                }

                if (ch == '=')
                {
                    return new[]
                    {
                        pair.Substring(0, index),
                        pair.Substring(index + 1)
                    };
                }
            }

            return new[] { pair ?? string.Empty };
        }
    }
}
