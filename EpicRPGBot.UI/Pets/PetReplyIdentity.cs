using System;
using System.Text.RegularExpressions;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Pets
{
    public static class PetReplyIdentity
    {
        public static bool IsEpic(DiscordMessageSnapshot reply)
        {
            if (reply == null) return false;
            var author = Normalize(reply.Author);
            if (author.Length > 0) return IsEpicName(author);
            // Discord can omit the author field while keeping the message header
            // in rendered text. Only inspect the leading header, never an embed/body mention.
            var header = Regex.Split(Normalize(reply.Text), @"\r?\n")[0].Trim();
            return Regex.IsMatch(header, @"^EPIC\s+RPG(?:\s*(?:Verified\s+App|APP|BOT))*(?:\s+\d{1,2}:\d{2})?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool IsEpicName(string author) => Regex.IsMatch(author,
            @"^EPIC\s+RPG(?:\s*(?:Verified\s+App|APP|BOT))*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string Normalize(string value) => Regex.Replace(value ?? "",
            @"[\u200b-\u200f\u202a-\u202e\u2060-\u206f\ufeff\u2713\u2714\ufe0f]", "").Trim();
    }
}
