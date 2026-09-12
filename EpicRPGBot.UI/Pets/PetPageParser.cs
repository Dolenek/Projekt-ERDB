using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Pets
{
    public sealed class PetPage
    {
        public string Owner { get; set; }
        public int Total { get; set; }
        public int Number { get; set; }
        public int Pages { get; set; }
        public IReadOnlyList<PetRecord> Pets { get; set; }
    }

    public static class PetPageParser
    {
        private const RegexOptions Flags = RegexOptions.IgnoreCase | RegexOptions.Multiline;
        public static PetPage Parse(string text)
        {
            text = (text ?? "").Replace("\r", "").Replace("**", "").Replace("__", "").Replace("`", "");
            text = Regex.Replace(text, @":[a-zA-Z0-9_]+:", "");
            var owner = Regex.Match(text, @"^\s*(?<v>[^\n]+?)\s*[—–-]\s*pets\s*$", Flags);
            var total = Regex.Match(text, @"Total pets:\s*(\d+)\s*/\s*\d+", Flags);
            var page = Regex.Match(text, @"Page:\s*(\d+)\s*/\s*(\d+)", Flags);
            if (!owner.Success || !total.Success || !page.Success) return null;
            if (!int.TryParse(total.Groups[1].Value, out var count) ||
                !int.TryParse(page.Groups[1].Value, out var number) ||
                !int.TryParse(page.Groups[2].Value, out var pages) ||
                number < 1 || pages < number || pages > 1000) return null;
            var blocks = Regex.Matches(text, @"\bID:\s*([a-z0-9]+)([\s\S]*?)(?=\bID:|Pets commands|\z)", Flags);
            var pets = blocks.Cast<Match>().Select(ParsePet).ToArray();
            return new PetPage { Owner = owner.Groups["v"].Value.Trim(), Total = count,
                Number = number, Pages = pages, Pets = pets };
        }

        private static PetRecord ParsePet(Match block)
        {
            var body = block.Groups[2].Value;
            var species = Regex.Match(body, @"([a-z][a-z -]*?)\s*[—–-]\s*TIER\s+([IVX]+)\b", Flags);
            var score = Regex.Match(body, @"Pet score:\s*([\d,]+)", Flags);
            var status = Regex.Match(body, @"Status:\s*([^\n]+)", Flags);
            var skills = status.Success ? body.Substring(status.Index + status.Length).Trim() : "";
            var ranks = Regex.Matches(skills, @"([a-z][a-z ]*?)\s*\[(SS\+|SS|S|A|B|C|D|E|F)\]", Flags);
            var normalizedSkills = string.Join(", ", ranks.Cast<Match>()
                .Select(m => m.Groups[1].Value.Trim() + " [" + m.Groups[2].Value.ToUpperInvariant() + "]")
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
            // Keep unranked special skills and unknown text in the identity, too.
            var unranked = Regex.Replace(skills, @"[a-z][a-z ]*?\s*\[(SS\+|SS|S|A|B|C|D|E|F)\]", "", Flags);
            unranked = Regex.Replace(unranked, @"[^\p{L} ]", " ").Trim();
            if (unranked.Length > 0) normalizedSkills += " " + Regex.Replace(unranked, @"\s+", " ");
            var tier = RomanTier(species.Groups[2].Value);
            var validScore = int.TryParse(score.Groups[1].Value.Replace(",", ""), out var scoreValue);
            var unsupportedRank = Regex.Replace(skills, @"\[(SS\+|SS|S|A|B|C|D|E|F)\]", "", Flags)
                .IndexOfAny(new[] { '[', ']' }) >= 0;
            return new PetRecord { Id = block.Groups[1].Value.ToUpperInvariant(),
                Species = species.Groups[1].Value.Trim(), Tier = tier, Score = scoreValue,
                Status = status.Groups[1].Value.Trim(), Skills = normalizedSkills.Trim(),
                Recognized = species.Success && tier > 0 && validScore && status.Success &&
                    PetRecognitionCatalog.Recognizes(species.Groups[1].Value.Trim(), normalizedSkills.Trim()) &&
                    !unsupportedRank };
        }

        public static int RomanTier(string roman)
        {
            var tiers = new[] { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
                "XI", "XII", "XIII", "XIV", "XV", "XVI", "XVII", "XVIII", "XIX", "XX",
                "XXI", "XXII", "XXIII", "XXIV", "XXV" };
            return Array.IndexOf(tiers, roman.ToUpperInvariant()) + 1;
        }
    }
}
