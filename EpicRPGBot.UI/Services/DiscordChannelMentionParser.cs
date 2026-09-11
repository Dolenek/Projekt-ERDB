using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordChannelMentionParser
    {
        public static IReadOnlyDictionary<string, int> Parse(string payload)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(payload))
            {
                return counts;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (TryReadCount(property.Value, out var count))
                    {
                        counts[property.Name] = Math.Max(0, count);
                    }
                }
            }
            catch
            {
            }

            return counts;
        }

        private static bool TryReadCount(JsonElement value, out int count)
        {
            return value.ValueKind == JsonValueKind.Number
                ? value.TryGetInt32(out count)
                : int.TryParse(value.ToString(), out count);
        }
    }
}
