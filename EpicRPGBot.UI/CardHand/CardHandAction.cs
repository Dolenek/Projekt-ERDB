namespace EpicRPGBot.UI.CardHand
{
    public enum CardHandActionKind
    {
        Pass,
        Discard
    }

    public sealed class CardHandAction
    {
        private CardHandAction(CardHandActionKind kind, CardId discardedCard)
        {
            Kind = kind;
            DiscardedCard = discardedCard;
        }

        public CardHandActionKind Kind { get; }
        public CardId DiscardedCard { get; }
        public static CardHandAction Pass { get; } = new CardHandAction(CardHandActionKind.Pass, default);
        public static CardHandAction Discard(CardId card) => new CardHandAction(CardHandActionKind.Discard, card);
        public override string ToString() => Kind == CardHandActionKind.Pass ? "pass" : "discard " + DiscardedCard;
    }
}
