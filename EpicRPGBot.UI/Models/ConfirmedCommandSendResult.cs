namespace EpicRPGBot.UI.Models
{
    public sealed class ConfirmedCommandSendResult
    {
        public ConfirmedCommandSendResult(
            DiscordMessageSnapshot outgoingMessage,
            DiscordMessageSnapshot replyMessage,
            int attemptCount)
        {
            OutgoingMessage = outgoingMessage;
            ReplyMessage = replyMessage;
            AttemptCount = attemptCount;
        }

        public DiscordMessageSnapshot OutgoingMessage { get; }
        public DiscordMessageSnapshot ReplyMessage { get; }
        public int AttemptCount { get; }
        public bool IsConfirmed => OutgoingMessage != null && ReplyMessage != null;
    }
}
