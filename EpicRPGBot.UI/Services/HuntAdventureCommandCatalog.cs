using System;

namespace EpicRPGBot.UI.Services
{
    public static class HuntAdventureCommandCatalog
    {
        public static string ResolveHunt(bool useHardcore)
        {
            return useHardcore ? "rpg hunt h" : "rpg hunt";
        }

        public static string ResolveAdventure(bool useHardcore)
        {
            return useHardcore ? "rpg adv h" : "rpg adv";
        }

        public static bool ShouldSendHealAfter(string command, bool healEnabled)
        {
            if (!healEnabled)
            {
                return false;
            }

            var normalized = (command ?? string.Empty).Trim();
            return string.Equals(normalized, "rpg hunt", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "rpg hunt h", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "rpg adv", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalized, "rpg adv h", StringComparison.OrdinalIgnoreCase);
        }
    }
}
