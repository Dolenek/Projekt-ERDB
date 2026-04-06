using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordScriptParsing
    {
        public static string EscapeMessage(string message)
        {
            return (message ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        public static double ParseDouble(string raw)
        {
            double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value);
            return value;
        }

        public static string ExtractField(string json, string field)
        {
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var key = $"\"{field}\":";
            var startIndex = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (startIndex < 0)
            {
                return null;
            }

            startIndex += key.Length;
            while (startIndex < json.Length && json[startIndex] == ' ')
            {
                startIndex++;
            }

            var quoted = startIndex < json.Length && json[startIndex] == '"';
            if (quoted)
            {
                startIndex++;
            }

            var current = startIndex;
            if (quoted)
            {
                while (current < json.Length)
                {
                    if (json[current] == '\\')
                    {
                        current += 2;
                        continue;
                    }

                    if (json[current] == '"')
                    {
                        break;
                    }

                    current++;
                }

                return json.Substring(startIndex, Math.Max(0, current - startIndex))
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            while (current < json.Length && json[current] != ',' && json[current] != '}' && !char.IsWhiteSpace(json[current]))
            {
                current++;
            }

            return json.Substring(startIndex, Math.Max(0, current - startIndex)).Trim();
        }

        public static string UnquoteJson(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            value = value.Trim();
            try
            {
                using (var document = JsonDocument.Parse(value))
                {
                    if (document.RootElement.ValueKind == JsonValueKind.String)
                    {
                        return document.RootElement.GetString() ?? string.Empty;
                    }

                    return document.RootElement.GetRawText();
                }
            }
            catch
            {
                return value;
            }
        }

        public static IReadOnlyList<DiscordMessageSnapshot> ParseSnapshots(string payload)
        {
            var snapshots = new List<DiscordMessageSnapshot>();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return snapshots;
            }

            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        return snapshots;
                    }

                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        var snapshot = ParseSnapshot(item);
                        if (snapshot != null && !string.IsNullOrWhiteSpace(snapshot.Id))
                        {
                            snapshots.Add(snapshot);
                        }
                    }
                }
            }
            catch
            {
                return snapshots;
            }

            return snapshots;
        }

        public static DiscordMessageSnapshot ParseSnapshot(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    return ParseSnapshot(document.RootElement);
                }
            }
            catch
            {
                return null;
            }
        }

        private static DiscordMessageSnapshot ParseSnapshot(JsonElement item)
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return new DiscordMessageSnapshot(
                GetString(item, "id"),
                GetString(item, "text"),
                GetString(item, "author"),
                GetString(item, "renderedText"),
                ParseButtons(item));
        }

        private static IReadOnlyList<DiscordMessageButton> ParseButtons(JsonElement item)
        {
            if (!item.TryGetProperty("buttons", out var buttonsElement) ||
                buttonsElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<DiscordMessageButton>();
            }

            var buttons = new List<DiscordMessageButton>();
            foreach (var button in buttonsElement.EnumerateArray())
            {
                if (button.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                buttons.Add(new DiscordMessageButton(
                    GetString(button, "label"),
                    GetInt32(button, "rowIndex"),
                    GetInt32(button, "columnIndex")));
            }

            return buttons;
        }

        private static string GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property) ||
                property.ValueKind == JsonValueKind.Null ||
                property.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            return property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? string.Empty
                : property.ToString();
        }

        private static int GetInt32(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var property))
            {
                return 0;
            }

            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value))
            {
                return value;
            }

            return int.TryParse(property.ToString(), out value) ? value : 0;
        }
    }
}
