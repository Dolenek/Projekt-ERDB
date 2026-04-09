using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonEntryReplyParser
    {
        public DungeonEntryReplyKind Parse(DiscordMessageSnapshot snapshot)
        {
            var message = snapshot?.Text ?? string.Empty;
            var renderedText = snapshot?.RenderedText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(message) && string.IsNullOrWhiteSpace(renderedText))
            {
                return DungeonEntryReplyKind.Unknown;
            }

            if (message.IndexOf("middle of a command", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return DungeonEntryReplyKind.PartnerBusy;
            }

            var hasEntryText =
                message.IndexOf("ARE YOU SURE YOU WANT TO ENTER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                renderedText.IndexOf("ARE YOU SURE YOU WANT TO ENTER", StringComparison.OrdinalIgnoreCase) >= 0;
            return hasEntryText && DungeonMessageInteraction.HasButton(snapshot, "yes")
                ? DungeonEntryReplyKind.EntryPrompt
                : DungeonEntryReplyKind.Unknown;
        }
    }
}
