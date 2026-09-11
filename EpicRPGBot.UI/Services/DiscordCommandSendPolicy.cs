using System;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordCommandSendPolicy
    {
        private const string DungeonEntryCommand = "rpg dung";
        private const string DuelCommand = "rpg duel";

        public static string GetOutgoingDetectionToken(string command)
        {
            var normalizedCommand = NormalizeWhitespace(command);
            if (IsDungeonEntryCommand(normalizedCommand))
            {
                return DungeonEntryCommand;
            }

            return IsDuelCommand(normalizedCommand) ? DuelCommand : normalizedCommand;
        }

        public static bool AllowsBlindResend(string command)
        {
            var normalizedCommand = NormalizeWhitespace(command);
            return !IsDungeonEntryCommand(normalizedCommand) && !IsDuelCommand(normalizedCommand);
        }

        private static bool IsDungeonEntryCommand(string normalizedCommand)
        {
            return normalizedCommand.StartsWith(
                DungeonEntryCommand + " ",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDuelCommand(string normalizedCommand)
        {
            return normalizedCommand.StartsWith(
                DuelCommand + " ",
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
