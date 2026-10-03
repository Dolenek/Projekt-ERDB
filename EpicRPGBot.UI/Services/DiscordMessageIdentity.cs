using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordMessageIdentity
    {
        private const string EpicNamePattern = @"EPIC\s+RPG(?:\s*(?:Verified\s+App|APP|BOT))*";
        private const RegexOptions NameOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        public static bool IsEpic(DiscordMessageSnapshot message)
        {
            if (message == null) return false;
            var author = Normalize(message.Author);
            if (author.Length > 0) return Regex.IsMatch(author, "^" + EpicNamePattern + "$", NameOptions);
            return HasLeadingEpicHeader(message.Text) || HasLeadingEpicHeader(message.RenderedText);
        }

        private static bool HasLeadingEpicHeader(string text)
        {
            var header = Regex.Split(Normalize(text), @"\r?\n")[0].Trim();
            return Regex.IsMatch(header, "^" + EpicNamePattern + @"(?:\s+\d{1,2}:\d{2})?$", NameOptions);
        }

        private static string Normalize(string value)
        {
            return Regex.Replace(value ?? string.Empty,
                @"[\u200b-\u200f\u202a-\u202e\u2060-\u206f\ufeff\u2713\u2714\ufe0f]", string.Empty).Trim();
        }
    }
}
