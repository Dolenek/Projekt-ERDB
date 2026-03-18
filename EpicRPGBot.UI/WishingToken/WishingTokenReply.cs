namespace EpicRPGBot.UI.WishingToken
{
    public sealed class WishingTokenReply
    {
        public WishingTokenReply(WishingTokenReplyKind kind, string summary)
        {
            Kind = kind;
            Summary = summary ?? string.Empty;
        }

        public WishingTokenReplyKind Kind { get; }
        public string Summary { get; }
    }
}
