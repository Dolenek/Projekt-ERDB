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
            return DiscordMessageChronology.IsAfter(replyId, outgoingId);
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
                var reply = DiscordCommandReplySelector.SelectFirst(messages, outgoingId);
                if (reply != null) return reply;
                await Task.Delay(250, token);
            }
            return null;
        }
    }
}
