using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class DungeonEntryPreparation
    {
        private DungeonEntryPreparation(DiscordMessageMention partner, DiscordMessageSnapshot confirmationPrompt)
        {
            Partner = partner;
            ConfirmationPrompt = confirmationPrompt;
        }

        public DiscordMessageMention Partner { get; }

        public DiscordMessageSnapshot ConfirmationPrompt { get; }

        public bool HasConfirmationPrompt => ConfirmationPrompt != null;

        public static DungeonEntryPreparation ForPartner(DiscordMessageMention partner)
        {
            if (partner == null)
            {
                throw new ArgumentNullException(nameof(partner));
            }

            return new DungeonEntryPreparation(partner, null);
        }

        public static DungeonEntryPreparation ForConfirmationPrompt(DiscordMessageSnapshot confirmationPrompt)
        {
            if (confirmationPrompt == null)
            {
                throw new ArgumentNullException(nameof(confirmationPrompt));
            }

            return new DungeonEntryPreparation(null, confirmationPrompt);
        }
    }
}
