using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Pets
{
    public static class PetRecognitionCatalog
    {
        private static readonly string[] Species = { "Cat", "Dog", "Dragon", "Bunny", "Pony", "Worker", "Snowman" };
        private static readonly string[] Skills = { "Time Traveler", "Fast", "Happy", "Clever", "Digger", "Lucky",
            "EPIC", "ASCENDED", "PERFECT", "FIGHTER", "MASTER", "Normie", "Farmer", "Leader", "Gifter" };

        public static bool Recognizes(string species, string skills)
        {
            if (!Species.Contains(species, StringComparer.OrdinalIgnoreCase)) return false;
            var remaining = skills;
            foreach (var skill in Skills)
                remaining = Regex.Replace(remaining, @"\b" + skill + @"\s*\[(SS\+|SS|S|A|B|C|D|E|F)\]", "", RegexOptions.IgnoreCase);
            foreach (var unranked in new[] { "Normie", "Farmer", "Leader", "Gifter" })
                remaining = Regex.Replace(remaining, @"\b" + unranked + @"\b", "", RegexOptions.IgnoreCase);
            return Regex.IsMatch(remaining, @"^[\s,]*$");
        }
    }
}
