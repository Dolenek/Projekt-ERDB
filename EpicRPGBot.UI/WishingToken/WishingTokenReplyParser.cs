using System;

namespace EpicRPGBot.UI.WishingToken
{
    public sealed class WishingTokenReplyParser
    {
        public WishingTokenReply Parse(string message)
        {
            if (IsWishMenu(message))
            {
                return new WishingTokenReply(WishingTokenReplyKind.WishMenu, "Wish menu received.");
            }

            if (IsFailure(message))
            {
                return new WishingTokenReply(WishingTokenReplyKind.Failure, "Wish failed and returned a time cookie.");
            }

            if (IsSuccess(message))
            {
                return new WishingTokenReply(WishingTokenReplyKind.Success, "Wish succeeded and returned 30 time cookies.");
            }

            return new WishingTokenReply(WishingTokenReplyKind.Unknown, "Reply did not match a wishing-token result.");
        }

        private static bool IsWishMenu(string message)
        {
            return Contains(message, "Select an item to wish for") &&
                   Contains(message, "time cookie");
        }

        private static bool IsFailure(string message)
        {
            return Contains(message, "wish failed") &&
                   Contains(message, "Consolation prize") &&
                   Contains(message, "time cookie");
        }

        private static bool IsSuccess(string message)
        {
            return Contains(message, "got 30") &&
                   Contains(message, "time cookie");
        }

        private static bool Contains(string text, string value)
        {
            return !string.IsNullOrWhiteSpace(text) &&
                   text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
