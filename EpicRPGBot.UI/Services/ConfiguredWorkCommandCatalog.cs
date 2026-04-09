using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.Services
{
    internal static class ConfiguredWorkCommandCatalog
    {
        private static readonly string[] LegacyCommandTexts =
        {
            "rpg chop",
            "rpg fish",
            "rpg pickup",
            "rpg mine",
            "rpg axe",
            "rpg bowsaw",
            "rpg chainsaw"
        };

        private static readonly string[] LegacyCooldownAliases =
        {
            "chop",
            "fish",
            "pickup",
            "mine",
            "axe",
            "bowsaw",
            "chainsaw"
        };

        public static string[] BuildCooldownAliases(string serializedSelections)
        {
            var aliases = new HashSet<string>(LegacyCooldownAliases, StringComparer.OrdinalIgnoreCase);
            foreach (var commandText in EnumerateCommandTexts(serializedSelections))
            {
                var alias = NormalizeCooldownAlias(commandText);
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    aliases.Add(alias);
                }
            }

            var results = new string[aliases.Count];
            aliases.CopyTo(results);
            return results;
        }

        public static bool IsWorkCommand(string commandText, string serializedSelections)
        {
            var normalizedCommand = NormalizeCommandText(commandText);
            if (string.IsNullOrWhiteSpace(normalizedCommand))
            {
                return false;
            }

            var knownCommands = new HashSet<string>(LegacyCommandTexts, StringComparer.OrdinalIgnoreCase);
            foreach (var configuredCommand in EnumerateCommandTexts(serializedSelections))
            {
                knownCommands.Add(configuredCommand);
            }

            return knownCommands.Contains(normalizedCommand);
        }

        private static IEnumerable<string> EnumerateCommandTexts(string serializedSelections)
        {
            var selections = AreaWorkCommandSettings.Parse(serializedSelections);
            foreach (var commandText in selections.Values)
            {
                var normalized = NormalizeCommandText(commandText);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    yield return normalized;
                }
            }
        }

        private static string NormalizeCooldownAlias(string commandText)
        {
            var normalized = NormalizeCommandText(commandText);
            if (normalized.StartsWith("rpg ", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(4).Trim();
            }

            return normalized;
        }

        private static string NormalizeCommandText(string commandText)
        {
            return string.IsNullOrWhiteSpace(commandText) ? string.Empty : commandText.Trim();
        }
    }
}
