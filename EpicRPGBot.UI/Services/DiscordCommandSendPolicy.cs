using System;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordCommandSendPolicy
    {
        private const string DungeonEntryCommand = "rpg dung";

        public static string GetOutgoingDetectionToken(string command)
        {
            var normalizedCommand = NormalizeWhitespace(command);
            return IsDungeonEntryCommand(normalizedCommand)
                ? DungeonEntryCommand
                : normalizedCommand;
        }

        public static bool AllowsBlindResend(string command)
        {
            return !IsDungeonEntryCommand(NormalizeWhitespace(command));
        }

        private static bool IsDungeonEntryCommand(string normalizedCommand)
        {
            return normalizedCommand.StartsWith(
                DungeonEntryCommand + " ",
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeWhitespace(string value)
        {
            return string.Join(
                " ",
                (value ?? string.Empty).Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
