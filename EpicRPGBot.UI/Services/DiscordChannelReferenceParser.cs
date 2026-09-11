using System;
using System.Collections.Generic;
using System.Text.Json;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordChannelReferenceParser
    {
        public static IReadOnlyList<DiscordChannelReference> Parse(string payload)
        {
            var channels = new List<DiscordChannelReference>();
            if (string.IsNullOrWhiteSpace(payload))
            {
                return channels;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    var id = Read(item, "id");
                    var name = Read(item, "name");
                    var url = Read(item, "url");
                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                    {
                        channels.Add(new DiscordChannelReference(id, name, url));
                    }
                }
            }
            catch
            {
            }

            return channels;
        }

        private static string Read(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property)
                ? property.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
