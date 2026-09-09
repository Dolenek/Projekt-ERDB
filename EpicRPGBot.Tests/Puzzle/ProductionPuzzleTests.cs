using EpicRPGBot.UI.Puzzle;
using EpicRPGBot.UI.Puzzle.Local;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class ProductionPuzzleTests
{
    [WindowsFact]
    public async Task ShippedPolicy_ValidatesAllSixteenClassesAndAllowsCanonicalKeyAnswer()
    {
        var root = LocalPuzzleTransformTests.RepositoryRoot();
        var policy = LocalPuzzlePolicy.Load(Path.Combine(root, "puzzle-local.json"));
        Assert.Equal(LocalPuzzlePolicy.FinePipeline, policy.Pipeline);
        Assert.Equal(100, policy.TestTotal);
        Assert.Equal(100, policy.TestCorrect);
        Assert.Equal(0, policy.TestWrong);
        using var provider = new LocalPuzzleAnswerProvider(Path.Combine(root, "Items"),
            PuzzleItemCatalog.Load(Path.Combine(root, "items.json")), policy);
        Assert.Equal(16, provider.Labels.Count);
        Assert.True(provider.AutomaticAnswersValidated);
        Assert.Equal(policy.ValidatedFingerprint, provider.Fingerprint);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/puzzle-dataset-expansion-20260908/images/1485124172236456016.png"));
        var result = await provider.SolveAsync(bytes, default);
        Assert.Equal("key", result.Label);
        Assert.True(result.AutomaticSubmissionAllowed);
    }
}
