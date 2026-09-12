using System;
using System.Linq;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public static class DiscordMessagePermalink
    {
        private static readonly Regex SnowflakePattern =
            new Regex(
                "^(?:chat-messages-(?:\\d{15,22}-)?)?(\\d{15,22})$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool TryBuild(DiscordMessageReference reference, out string permalink)
        {
            permalink = string.Empty;
            if (reference?.IsComplete != true ||
                !TryReadChannel(reference.ChannelUrl, out var origin, out var scopeId, out var channelId) ||
                !TryExtractSnowflake(reference.MessageElementId, out var messageId))
            {
                return false;
            }

            permalink = $"{origin}/channels/{scopeId}/{channelId}/{messageId}";
            return true;
        }

        public static bool TryExtractSnowflake(string messageElementId, out string messageId)
        {
            var match = SnowflakePattern.Match(messageElementId ?? string.Empty);
            messageId = match.Success ? match.Groups[1].Value : string.Empty;
            return match.Success;
        }

        private static bool TryReadChannel(
            string channelUrl,
            out string origin,
            out string scopeId,
            out string channelId)
        {
            origin = scopeId = channelId = string.Empty;
            if (!Uri.TryCreate(channelUrl, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !IsDiscordHost(uri.Host))
            {
                return false;
            }

            var segments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length < 3 ||
                !string.Equals(segments[0], "channels", StringComparison.OrdinalIgnoreCase) ||
                !IsScopeId(segments[1]) ||
                !IsSnowflake(segments[2]))
            {
                return false;
            }

            origin = uri.GetLeftPart(UriPartial.Authority);
            scopeId = segments[1];
            channelId = segments[2];
            return true;
        }

        private static bool IsDiscordHost(string host)
        {
            return string.Equals(host, "discord.com", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "canary.discord.com", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "ptb.discord.com", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsScopeId(string value)
        {
            return string.Equals(value, "@me", StringComparison.OrdinalIgnoreCase) || IsSnowflake(value);
        }

        private static bool IsSnowflake(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.Length >= 15 &&
                   value.Length <= 22 &&
                   value.All(char.IsDigit);
        }
    }
}
