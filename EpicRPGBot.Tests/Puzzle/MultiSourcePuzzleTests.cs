using EpicRPGBot.UI.Puzzle.Local;
using OpenCvSharp;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class MultiSourcePuzzleTests
{
    [WindowsFact]
    public void NonWinningReference_RetainsItsOwnRefinementSeed()
    {
        using var card = new Mat(200, 500, MatType.CV_8UC3, Scalar.All(34));
        Cv2.Rectangle(card, new Rect(40, 60, 40, 40), Scalar.All(180), -1);
        Cv2.Rectangle(card, new Rect(55, 75, 10, 10), Scalar.All(70), -1);
        Cv2.ImEncode(".png", card, out var bytes);
        using var scene = PuzzleScene.Decode(bytes);
        using var exact = new Mat(scene.Colors, new Rect(25, 29, 20, 20));
        using var approximate = exact.Clone();
        Cv2.Rectangle(approximate, new Rect(2, 2, 4, 4), Scalar.All(100), -1);
        using var winner = new PuzzleTemplateVariant("normie fish", exact);
        using var alternative = new PuzzleTemplateVariant("normie fish", approximate,
            sourceKey: "normie fish/alternative.webp");
        using var legacy = new PuzzleTemplateSearch(scene, spectral: true);
        using var multiple = new PuzzleTemplateSearch(scene, spectral: true, multipleSources: true);
        foreach (var search in new[] { legacy, multiple })
        {
            search.Evaluate(winner);
            search.Evaluate(alternative);
        }
        Assert.Single(legacy.Seeds);
        Assert.Equal(2, multiple.Seeds.Count());
        Assert.Single(multiple.Candidates);
        Assert.Equal(legacy.Candidates.Single().Score, multiple.Candidates.Single().Score);
    }

    [WindowsFact]
    public void Refinement_CancellationIsObservedBeforeTransformingReferences()
    {
        using var card = new Mat(200, 500, MatType.CV_8UC3, Scalar.All(34));
        Cv2.Rectangle(card, new Rect(40, 60, 40, 40), Scalar.All(180), -1);
        Cv2.ImEncode(".png", card, out var bytes);
        using var scene = PuzzleScene.Decode(bytes);
        using var icon = new Mat(scene.Colors, new Rect(25, 29, 20, 20));
        using var variant = new PuzzleTemplateVariant("normie fish", icon);
        using var search = new PuzzleTemplateSearch(scene);
        search.Evaluate(variant);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        IPuzzlePoseRefiner refiner = new PuzzlePoseRefiner();
        Assert.Throws<OperationCanceledException>(() =>
            refiner.Refine(search, null!, cancellation.Token, 3, 1, .05, false));
    }

    [Fact]
    public void MultipleReferenceSearch_RequiresItsOwnValidation()
    {
        var policy = new LocalPuzzlePolicy { Pipeline = LocalPuzzlePolicy.GlyphPipeline };
        var previous = policy.Fingerprint("same-templates");
        policy.Pipeline = LocalPuzzlePolicy.MultiSourcePipeline;
        Assert.NotEqual(previous, policy.Fingerprint("same-templates"));
        Assert.True(LocalPuzzlePolicy.IsSupportedPipeline(policy.Pipeline));
    }
}
