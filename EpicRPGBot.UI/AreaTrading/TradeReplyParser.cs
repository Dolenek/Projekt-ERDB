using System;

namespace EpicRPGBot.UI.AreaTrading
{
    public sealed class TradeReplyParser
    {
        public TradeReply Parse(string replyText)
        {
            var text = replyText ?? string.Empty;

            if (ContainsAny(text, "amount has to be 1 or higher"))
            {
                return new TradeReply(TradeReplyKind.InvalidAmount, text);
            }

            if (ContainsAny(text, "wait at least"))
            {
                return new TradeReply(TradeReplyKind.Wait, text);
            }

            if (ContainsAny(
                text,
                "don't have enough",
                "not enough items",
                "don't have any",
                "you have no"))
            {
                return new TradeReply(TradeReplyKind.MissingItems, text);
            }

            if (ContainsAny(
                text,
                "successfully traded",
                "you traded",
                "traded"))
            {
                return new TradeReply(TradeReplyKind.Success, text);
            }

            return new TradeReply(TradeReplyKind.Unknown, text);
        }

        private static bool ContainsAny(string text, params string[] fragments)
        {
            for (var i = 0; i < fragments.Length; i++)
            {
                if (text.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
