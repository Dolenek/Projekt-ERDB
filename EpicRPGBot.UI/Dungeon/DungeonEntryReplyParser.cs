using System;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonEntryReplyParser
    {
        public DungeonEntryReplyKind Parse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return DungeonEntryReplyKind.Unknown;
            }

            return message.IndexOf("middle of a command", StringComparison.OrdinalIgnoreCase) >= 0
                ? DungeonEntryReplyKind.PartnerBusy
                : DungeonEntryReplyKind.Unknown;
        }
    }
}
