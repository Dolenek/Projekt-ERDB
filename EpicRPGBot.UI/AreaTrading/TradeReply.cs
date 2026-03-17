namespace EpicRPGBot.UI.AreaTrading
{
    public sealed class TradeReply
    {
        public TradeReply(TradeReplyKind kind, string text)
        {
            Kind = kind;
            Text = text ?? string.Empty;
        }

        public TradeReplyKind Kind { get; }
        public string Text { get; }
    }
}
