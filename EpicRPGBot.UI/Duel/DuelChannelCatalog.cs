using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelChannelCatalog
    {
        public const string GuildId = "792124157117988907";
        public const string CategoryId = "792253026873245708";
        public const string OutgoingDuelChannelId = "792125583374024754";
        public const string OutgoingDuelChannelUrl =
            "https://discord.com/channels/792124157117988907/792125583374024754";

        private static readonly DuelChannelBand[] Bands =
        {
            new DuelChannelBand("road-to-50", 1, 49),
            new DuelChannelBand("road-to-200", 50, 199),
            new DuelChannelBand("road-to-500", 200, 499),
            new DuelChannelBand("road-to-1500", 500, 1499),
            new DuelChannelBand("road-to-3000", 1500, 2999),
            new DuelChannelBand("road-to-6000+", 3000, null)
        };

        private static readonly DiscordChannelReference[] FixedChannels =
        {
            FixedChannel("1262467703902437456", "road-to-50"),
            FixedChannel("1262468343135342703", "road-to-200"),
            FixedChannel("1262468897974517840", "road-to-500"),
            FixedChannel("1262468944208592898", "road-to-1500"),
            FixedChannel("1262469016803610775", "road-to-3000"),
            FixedChannel("1262476104564740199", "road-to-6000+"),
            FixedChannel("792125562557562880", "dueling-1"),
            FixedChannel(OutgoingDuelChannelId, "dueling-2"),
            FixedChannel("1036018255812366377", "dueling-3"),
            FixedChannel("1062896821950742638", "dueling-4")
        };

        public static bool IsAllowedTargetUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl) ||
                !string.Equals(parsedUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(parsedUrl.Host, "discord.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var pathParts = parsedUrl.AbsolutePath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return pathParts.Length == 3 &&
                   string.Equals(pathParts[0], "channels", StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(pathParts[1], GuildId, StringComparison.Ordinal) &&
                   FixedChannels.Any(channel => string.Equals(channel.Id, pathParts[2], StringComparison.Ordinal));
        }

        public static string DescribeTargetUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl))
            {
                return "unknown channel";
            }

            var channelId = parsedUrl.AbsolutePath
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();
            var channel = FixedChannels.FirstOrDefault(candidate =>
                string.Equals(candidate.Id, channelId, StringComparison.Ordinal));
            return channel == null
                ? $"channel {channelId ?? "unknown"}"
                : $"#{channel.Name} ({channel.Id})";
        }

        public IReadOnlyList<DiscordChannelReference> MergeFixedChannels(
            IReadOnlyList<DiscordChannelReference> discoveredChannels)
        {
            return (discoveredChannels ?? Array.Empty<DiscordChannelReference>())
                .Concat(FixedChannels)
                .Where(channel => channel != null && !string.IsNullOrWhiteSpace(channel.Name))
                .GroupBy(channel => Normalize(channel.Name), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.Last())
                .ToArray();
        }

        public IReadOnlyList<DuelChannelBand> GetRelevantBands(int playerLevel)
        {
            var minimum = (playerLevel + 1) / 2;
            var maximum = (long)playerLevel * 2;
            return Bands.Where(band => band.Intersects(minimum, maximum)).ToArray();
        }

        public DuelChannelBand GetPlayerBand(int playerLevel)
        {
            return Bands.Single(band => band.Contains(playerLevel));
        }

        public IReadOnlyList<DiscordChannelReference> ResolveRelevantListingChannels(
            int playerLevel,
            IReadOnlyList<DiscordChannelReference> channels)
        {
            var lookup = BuildLookup(channels);
            return GetRelevantBands(playerLevel)
                .Select(band => ResolveRequired(lookup, band.ChannelName))
                .ToArray();
        }

        public DiscordChannelReference ResolvePlayerListingChannel(
            int playerLevel,
            IReadOnlyList<DiscordChannelReference> channels)
        {
            return ResolveRequired(BuildLookup(channels), GetPlayerBand(playerLevel).ChannelName);
        }

        public IReadOnlyList<DiscordChannelReference> ResolveDuelingChannels(
            IReadOnlyList<DiscordChannelReference> channels)
        {
            var lookup = BuildLookup(channels);
            return Enumerable.Range(1, 4)
                .Select(index => ResolveRequired(lookup, "dueling-" + index))
                .ToArray();
        }

        public DiscordChannelReference ResolveOutgoingDuelChannel(
            IReadOnlyList<DiscordChannelReference> channels)
        {
            var channel = ResolveRequired(BuildLookup(channels), "dueling-2");
            if (!string.Equals(channel.Id, OutgoingDuelChannelId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Required duel channel 'dueling-2' has unexpected id '{channel.Id}'.");
            }

            return channel;
        }

        private static Dictionary<string, DiscordChannelReference> BuildLookup(
            IReadOnlyList<DiscordChannelReference> channels)
        {
            return (channels ?? Array.Empty<DiscordChannelReference>())
                .Where(channel => channel != null && !string.IsNullOrWhiteSpace(channel.Name))
                .GroupBy(channel => Normalize(channel.Name))
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        private static DiscordChannelReference ResolveRequired(
            IReadOnlyDictionary<string, DiscordChannelReference> lookup,
            string name)
        {
            if (lookup.TryGetValue(Normalize(name), out var channel))
            {
                return channel;
            }

            throw new InvalidOperationException($"Required duel channel '{name}' was not found.");
        }

        private static string Normalize(string name)
        {
            return new string((name ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Where(character => char.IsLetterOrDigit(character) || character == '+')
                .ToArray());
        }

        private static DiscordChannelReference FixedChannel(string id, string name)
        {
            return new DiscordChannelReference(
                id,
                name,
                $"https://discord.com/channels/{GuildId}/{id}");
        }
    }
}
