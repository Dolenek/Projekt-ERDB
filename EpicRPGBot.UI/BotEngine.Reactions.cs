using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private bool TryBeginReactiveHandling(DiscordMessageSnapshot snapshot)
        {
            return _messageReactionGate.TryBegin(snapshot?.Id);
        }
    }
}
