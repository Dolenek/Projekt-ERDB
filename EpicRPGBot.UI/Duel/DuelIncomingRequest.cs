using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelIncomingRequest
    {
        public DuelIncomingRequest(
            DiscordChannelReference channel,
            DuelMessageClassification classification)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Classification = classification ?? throw new ArgumentNullException(nameof(classification));
        }

        public DiscordChannelReference Channel { get; }

        public DuelMessageClassification Classification { get; }
    }
}
