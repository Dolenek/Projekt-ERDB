using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                value = value.Substring(1, value.Length - 2)
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\");
            }

            return value;
        }

        public static IReadOnlyList<DiscordMessageSnapshot> ParseSnapshots(string payload)
        {
            var snapshots = new List<DiscordMessageSnapshot>();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return snapshots;
            }

            var index = 0;
            while (index < payload.Length)
            {
                var start = payload.IndexOf('{', index);
                if (start < 0)
                {
                    break;
                }

                var depth = 0;
                var inString = false;
                var escape = false;
                var end = -1;
                for (var i = start; i < payload.Length; i++)
                {
                    var current = payload[i];
                    if (inString)
                    {
                        if (escape)
                        {
                            escape = false;
                        }
                        else if (current == '\\')
                        {
                            escape = true;
                        }
                        else if (current == '"')
                        {
                            inString = false;
                        }

                        continue;
                    }

                    if (current == '"')
                    {
                        inString = true;
                    }
                    else if (current == '{')
                    {
                        depth++;
                    }
                    else if (current == '}')
                    {
                        depth--;
                        if (depth == 0)
                        {
                            end = i;
                            break;
                        }
                    }
                }

                if (end < 0)
                {
                    break;
                }

                var item = payload.Substring(start, end - start + 1);
                snapshots.Add(new DiscordMessageSnapshot(
                    ExtractField(item, "id"),
                    ExtractField(item, "text"),
                    ExtractField(item, "author")));
                index = end + 1;
            }

            return snapshots
                .Where(snapshot => snapshot != null && !string.IsNullOrWhiteSpace(snapshot.Id))
                .ToList();
        }
    }
}
