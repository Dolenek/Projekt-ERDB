using EpicRPGBot.UI.CardHand;
using OpenCvSharp;
using Xunit;
using static EpicRPGBot.Tests.CardHand.CardHandTestCards;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardDeckImageParserTests
{
    [Fact]
    public void Parse_ReadsBrightDarkGoldenedAndJokerCells()
    {
        var owned = Cards("H2", "D5", "CQ", "SK", "JOKER");
        var png = BuildDeck(owned, Card("C7"));

        var result = new CardDeckImageParser().Parse(png);

        Assert.True(result.Success, result.Error);
        Assert.Equal(owned.Append(Card("C7")).OrderBy(card => card), result.OwnedCards.OrderBy(card => card));
    }

    [Fact]
    public void Parse_RejectsAnUnrelatedImage()
    {
        using var image = new Mat(new Size(100, 100), MatType.CV_8UC3, Scalar.Black);
        Cv2.ImEncode(".png", image, out var png);

        var result = new CardDeckImageParser().Parse(png);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Error);
    }

    [Fact]
    public void Parse_RejectsAnAmbiguousOwnershipCell()
    {
        var png = BuildDeck(Array.Empty<CardId>(), default);
        using var image = Cv2.ImDecode(png, ImreadModes.Color);
        DrawCell(image, StandardCell(0, 0), new Scalar(145, 145, 145));
        Cv2.ImEncode(".png", image, out var ambiguousPng);

        var result = new CardDeckImageParser().Parse(ambiguousPng);

        Assert.False(result.Success);
        Assert.Contains("H2", result.Error);
    }

    private static byte[] BuildDeck(IEnumerable<CardId> ownedCards, CardId goldened)
    {
        var owned = new HashSet<CardId>(ownedCards);
        using var image = new Mat(new Size(600, 435), MatType.CV_8UC3, new Scalar(0, 128, 0));
        var suits = new[] { CardSuit.Hearts, CardSuit.Diamonds, CardSuit.Clubs, CardSuit.Spades };
        for (var row = 0; row < suits.Length; row++)
        for (var column = 0; column < 13; column++)
        {
            var card = new CardId(suits[row], (CardRank)(column + 2));
            DrawCell(image, StandardCell(column, row), CellColor(card, owned, goldened));
        }

        DrawCell(image, new Rect(544, 300, 36, 57), CellColor(CardId.Joker, owned, goldened));
        Cv2.ImEncode(".png", image, out var png);
        return png;
    }

    private static Scalar CellColor(CardId card, ISet<CardId> owned, CardId goldened)
    {
        if (card == goldened) return new Scalar(0, 100, 130);
        return owned.Contains(card) ? new Scalar(240, 240, 240) : new Scalar(85, 85, 85);
    }

    private static Rect StandardCell(int column, int row)
    {
        return new Rect(20 + (int)Math.Round(column * 43.67d), 18 + (row * 70), 36, 63);
    }

    private static void DrawCell(Mat image, Rect bounds, Scalar color)
    {
        Cv2.Rectangle(image, bounds, color, -1);
    }
}
