using System;
using System.Text.RegularExpressions;

namespace EpicRPGBot.UI.Bunny
{
    public sealed class BunnyPromptParser
    {
        private static readonly Regex HappinessRegex = new Regex(@"Happiness\s*:\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex HungerRegex = new Regex(@"Hunger\s*:\s*(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PetTierRegex = new Regex(
            @"\b(?:DOG|CAT|DRAGON)\s+TIER(?:\s+[A-Z0-9]+)?\s+IS\s+APPROACHING\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public BunnyPromptParseResult Parse(string renderedMessage)
        {
            var message = renderedMessage ?? string.Empty;
            if (!LooksLikeBunnyPrompt(message))
            {
                return new BunnyPromptParseResult(false, false, 0, 0, string.Empty);
            }

            var happinessMatch = HappinessRegex.Match(message);
            var hungerMatch = HungerRegex.Match(message);
            if (happinessMatch.Success &&
                hungerMatch.Success &&
                int.TryParse(happinessMatch.Groups["value"].Value, out var happiness) &&
                int.TryParse(hungerMatch.Groups["value"].Value, out var hunger))
            {
                return new BunnyPromptParseResult(
                    true,
                    true,
                    happiness,
                    hunger,
                    $"Catch prompt recognized with Happiness {happiness} and Hunger {hunger}.");
            }

            return new BunnyPromptParseResult(
                true,
                false,
                0,
                0,
                "Catch prompt recognized, but numeric Happiness/Hunger values were unreadable.");
        }

        private static bool LooksLikeBunnyPrompt(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var hasBunnyFooter =
                message.IndexOf("How to get a bunny", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("rpg egg info bunny", StringComparison.OrdinalIgnoreCase) >= 0;
            var hasPetFooter = message.IndexOf("Use \"info\" to get information about pets", StringComparison.OrdinalIgnoreCase) >= 0;
            var hasPetTier = PetTierRegex.IsMatch(message);
            var hasFooter = hasBunnyFooter || (hasPetFooter && hasPetTier);
            if (!hasFooter)
            {
                return false;
            }

            return message.IndexOf("Happiness", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Hunger", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
