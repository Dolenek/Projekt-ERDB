using System;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelWeaponSelector
    {
        private static readonly string[] KnownWeapons =
        {
            "dagger", "shield", "anchor", "ruler", "creditcard", "tools", "magnet",
            "🗡", "🛡", "⚓", "📏", "💳", "🛠", "🧲"
        };

        public DiscordMessageButton Select(DiscordMessageSnapshot prompt)
        {
            var buttons = prompt?.Buttons?.ToArray() ?? Array.Empty<DiscordMessageButton>();
            var weaponButtons = buttons.Where(IsWeaponButton).ToArray();
            return weaponButtons.FirstOrDefault(IsCreditCard) ??
                   weaponButtons.FirstOrDefault() ??
                   buttons.FirstOrDefault(IsFallbackButton);
        }

        private static bool IsCreditCard(DiscordMessageButton button)
        {
            var normalized = Normalize(button?.Label);
            return normalized.Contains("creditcard") || normalized.Contains("💳");
        }

        private static bool IsWeaponButton(DiscordMessageButton button)
        {
            var normalized = Normalize(button?.Label);
            return KnownWeapons.Any(weapon => normalized.Contains(weapon));
        }

        private static bool IsFallbackButton(DiscordMessageButton button)
        {
            var normalized = Normalize(button?.Label);
            return normalized != "yes" && normalized != "no" &&
                   !normalized.Contains("reaction") && !string.IsNullOrWhiteSpace(normalized);
        }

        private static string Normalize(string value)
        {
            return new string((value ?? string.Empty)
                .Trim(':')
                .ToLowerInvariant()
                .Where(character => char.IsLetterOrDigit(character) || character > 127)
                .ToArray());
        }
    }
}
