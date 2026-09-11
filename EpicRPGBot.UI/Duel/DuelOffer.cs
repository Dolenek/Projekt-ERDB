using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelOffer
    {
        public DuelOffer(DiscordChannelReference channel, DiscordMessageSnapshot message, int level)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Level = level;
        }

        public DiscordChannelReference Channel { get; }

        public DiscordMessageSnapshot Message { get; }

        public int Level { get; }
    }
}
