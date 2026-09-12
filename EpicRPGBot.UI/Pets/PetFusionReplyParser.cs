using System;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Pets
{
    public static class PetFusionReplyParser
    {
        public static bool IsSuccess(string text, string owner)
        {
            text = (text ?? "").Replace("\r", "").Replace("`", "").Replace("**", "");
            var heading = Regex.Match(text, @"^\s*(?<owner>[^\n]+?)\s*[—–-]\s*pets\s*$", RegexOptions.Multiline);
            return heading.Success && string.Equals(heading.Groups["owner"].Value.Trim(), owner, StringComparison.Ordinal) &&
                Regex.IsMatch(text, @"^\s*You have got a new pet!\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase) &&
                Regex.IsMatch(text, @"\bID:\s*[A-Za-z0-9]+\b") &&
                Regex.IsMatch(text, @"[—–-]\s*TIER\s+[IVX]+\b", RegexOptions.IgnoreCase);
        }
    }
}
