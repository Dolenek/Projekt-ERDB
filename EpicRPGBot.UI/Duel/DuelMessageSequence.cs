using System;
using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    internal static class DuelMessageSequence
    {
        public static IReadOnlyList<DiscordMessageSnapshot> After(
            IReadOnlyList<DiscordMessageSnapshot> messages,
            string anchorMessageId)
        {
            if (messages == null || messages.Count == 0)
            {
                return Array.Empty<DiscordMessageSnapshot>();
            }

            var anchorIndex = -1;
            for (var index = 0; index < messages.Count; index++)
            {
                if (string.Equals(messages[index]?.Id, anchorMessageId, StringComparison.Ordinal))
                {
                    anchorIndex = index;
                    break;
                }
            }

            return messages.Skip(anchorIndex + 1).Where(message => message != null).ToArray();
        }

        public static string Signature(DuelMessageClassification classification)
        {
            var message = classification?.Message;
            var buttons = string.Join(",", message?.Buttons.Select(button => button.Label) ?? Array.Empty<string>());
            return $"{message?.Id}|{classification?.Kind}|{message?.RenderedText}|{buttons}";
        }
    }
}
