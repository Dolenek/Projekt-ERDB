using EpicRPGBot.UI.Puzzle.Local;
using OpenCvSharp;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class GlyphQuestionRegionTests
{
    [WindowsFact]
    public void BrightIconBesideText_RemainsInsideTheImageRegion()
    {
        using var card = new Mat(200, 700, MatType.CV_8UC3, Scalar.All(34));
        Cv2.Rectangle(card, new Rect(49, 67, 58, 37), Scalar.White, -1);
        Cv2.PutText(card, "what is the name of this item?", new Point(126, 97),
            HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2);
        IPuzzleQuestionRegionLocator locator = new PuzzleQuestionRegionLocator(new PuzzleGlyphMaskFilter());
        var bounds = locator.Locate(card);
        Assert.InRange(bounds.Right, 108, 126);
    }

    [WindowsFact]
    public void GlyphFiltering_DoesNotTreatAWhiteItemAsAQuestion()
    {
        using var card = new Mat(200, 700, MatType.CV_8UC3, Scalar.All(34));
        Cv2.Rectangle(card, new Rect(65, 65, 60, 45), Scalar.White, -1);
        var locator = new PuzzleQuestionRegionLocator(new PuzzleGlyphMaskFilter());
        Assert.Throws<ArgumentException>(() => locator.Locate(card));
    }

    [Fact]
    public void GlyphPipeline_CannotReuseThePreviousValidationSeal()
    {
        var policy = new LocalPuzzlePolicy { Pipeline = LocalPuzzlePolicy.FinePipeline };
        var previous = policy.Fingerprint("same-templates");
        policy.Pipeline = LocalPuzzlePolicy.GlyphPipeline;
        Assert.NotEqual(previous, policy.Fingerprint("same-templates"));
        Assert.True(LocalPuzzlePolicy.IsSupportedPipeline(policy.Pipeline));
    }
}
