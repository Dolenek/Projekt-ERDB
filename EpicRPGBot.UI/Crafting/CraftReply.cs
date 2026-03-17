namespace EpicRPGBot.UI.Crafting
{
    public enum CraftReplyKind
    {
        Success,
        MissingItems,
        Unknown
    }

    public sealed class CraftReply
    {
        public CraftReply(
            CraftReplyKind kind,
            string rawText,
            CraftItemDefinition missingItem = null,
            long availableAmount = 0,
            long requiredAmount = 0)
        {
            Kind = kind;
            RawText = rawText ?? string.Empty;
            MissingItem = missingItem;
            AvailableAmount = availableAmount;
            RequiredAmount = requiredAmount;
        }

        public CraftReplyKind Kind { get; }
        public string RawText { get; }
        public CraftItemDefinition MissingItem { get; }
        public long AvailableAmount { get; }
        public long RequiredAmount { get; }
    }
}
