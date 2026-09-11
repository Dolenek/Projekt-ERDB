using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    internal static class DuelButtonSelector
    {
        public static DiscordMessageButton Find(DiscordMessageSnapshot message, string label)
        {
            return message?.Buttons.FirstOrDefault(button => Normalize(button.Label) == Normalize(label));
        }

        private static string Normalize(string value)
        {
            return new string((value ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
    }
}
