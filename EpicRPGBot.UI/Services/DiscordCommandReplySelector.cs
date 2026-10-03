using System.Collections.Generic;
using System.Linq;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    internal static class DiscordCommandReplySelector
    {
        public static bool IsReplyFor(DiscordMessageSnapshot reply, string outgoingMessageId)
        {
            return DiscordMessageIdentity.IsEpic(reply) &&
                DiscordMessageChronology.IsAfter(reply.Id, outgoingMessageId);
        }

        public static DiscordMessageSnapshot SelectFirst(
            IEnumerable<DiscordMessageSnapshot> messages, string outgoingMessageId)
        {
            return messages?.Where(message => IsReplyFor(message, outgoingMessageId))
                .OrderBy(message => DiscordMessageChronology.GetSequence(message.Id)).FirstOrDefault();
        }
    }
}
