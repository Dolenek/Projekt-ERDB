using EpicRPGBot.UI.CardHand;

namespace EpicRPGBot.Tests.CardHand;

internal static class CardHandTestCards
{
    public static CardId Card(string code)
    {
        if (!CardId.TryParse(code, out var card)) throw new ArgumentException("Invalid test card: " + code);
        return card;
    }

    public static CardId[] Cards(params string[] codes) => codes.Select(Card).ToArray();
}
