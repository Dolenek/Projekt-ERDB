using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public static class GuildRaidResponseClassifier
    {
        public static bool IsGuardPrompt(DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            return ContainsGuardPrompt(snapshot.Text) ||
                   ContainsGuardPrompt(snapshot.RenderedText);
        }

        public static bool IsRaidConfirmation(DiscordMessageSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            var text = snapshot.Text ?? string.Empty;
            var renderedText = snapshot.RenderedText ?? string.Empty;
            return HasEpicReplyContext(snapshot, text, renderedText) &&
                   (ContainsRaidConfirmation(text) || ContainsRaidConfirmation(renderedText));
        }

        private static bool ContainsRaidConfirmation(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var hasGuildReward = message.IndexOf("Your guild earned", StringComparison.OrdinalIgnoreCase) >= 0;
            var hasRaidLabel = message.IndexOf("Raid", StringComparison.OrdinalIgnoreCase) >= 0;
            var hasEnergySection = message.IndexOf("Energy", StringComparison.OrdinalIgnoreCase) >= 0;
            return hasGuildReward || (hasRaidLabel && hasEnergySection);
        }

        private static bool ContainsGuardPrompt(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.IndexOf("EPIC GUARD: stop there,", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   message.IndexOf("Select the item of the image above or respond with the item name", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasEpicReplyContext(DiscordMessageSnapshot snapshot, string text, string renderedText)
        {
            return snapshot.Author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   renderedText.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
