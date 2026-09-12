using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Pets
{
    public static class PetReplyChronology
    {
        public static bool IsAfter(string replyId, string outgoingId)
        {
            if (string.IsNullOrWhiteSpace(replyId) || string.IsNullOrWhiteSpace(outgoingId)) return false;
            var replySeparator = replyId.LastIndexOf('-');
            var outgoingSeparator = outgoingId.LastIndexOf('-');
            if (!string.Equals(replyId.Substring(0, replySeparator + 1),
                outgoingId.Substring(0, outgoingSeparator + 1), StringComparison.Ordinal)) return false;
            return ulong.TryParse(replyId.Substring(replySeparator + 1), out var reply) &&
                ulong.TryParse(outgoingId.Substring(outgoingSeparator + 1), out var outgoing) && reply > outgoing;
        }

        public static async Task<DiscordMessageSnapshot> WaitAsync(IDiscordChatClient client,
            string outgoingId, CancellationToken token)
        {
            // Message DOM order can include an older quoted/replied-to message.
            // Snowflake ordering identifies new replies without sending the command again.
            for (var poll = 0; poll < 40; poll++)
            {
                token.ThrowIfCancellationRequested();
                var messages = await client.GetRecentMessagesAsync(30);
                var reply = messages.Where(m => IsAfter(m.Id, outgoingId) && PetReplyIdentity.IsEpic(m))
                    .OrderBy(m => ulong.Parse(m.Id.Substring(m.Id.LastIndexOf('-') + 1))).FirstOrDefault();
                if (reply != null) return reply;
                await Task.Delay(250, token);
            }
            return null;
        }
    }
}
