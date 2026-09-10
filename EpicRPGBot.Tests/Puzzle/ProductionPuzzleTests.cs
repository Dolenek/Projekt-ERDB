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
        var settings = PuzzleSettings.Load((key, fallback) => fallback, root);
        var policy = LocalPuzzlePolicy.Load(settings.PolicyFile);
        Assert.Equal(LocalPuzzlePolicy.MultiSourcePipeline, policy.Pipeline);
        Assert.Equal(200, policy.TestTotal);
        Assert.Equal(200, policy.TestCorrect);
        Assert.Equal(0, policy.TestWrong);
        using var provider = Assert.IsType<LocalPuzzleAnswerProvider>(new PuzzleProviderFactory().Create(settings));
        Assert.True(settings.AutomaticAnswersEnabled);
        Assert.Equal(16, provider.Labels.Count);
        Assert.True(provider.AutomaticAnswersValidated);
        Assert.Equal(policy.ValidatedFingerprint, provider.Fingerprint);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/puzzle-dataset-expansion-20260908/images/1485124172236456016.png"));
        var result = await provider.SolveAsync(bytes, default);
        Assert.Equal("key", result.Label);
        Assert.True(result.AutomaticSubmissionAllowed);
    }

    [Fact]
    public void ReplayDefaults_UseTheSameModelAsRuntime()
    {
        var root = LocalPuzzleTransformTests.RepositoryRoot();
        var settings = PuzzleSettings.Load((key, fallback) => fallback, root);
        var options = PuzzleReplay.ReplayOptions.Parse(new[] { root, "examples.json", "results.json" });
        Assert.Equal(Path.GetFullPath(settings.PolicyFile), Path.GetFullPath(options.PolicyPath));
        Assert.Equal(Path.GetFullPath(settings.TemplateDirectory), Path.GetFullPath(options.TemplateDirectory));
    }

    [Fact]
    public void EnvironmentOverrides_CanSelectPathsAndDisableSending()
    {
        var root = LocalPuzzleTransformTests.RepositoryRoot();
        var overrides = new Dictionary<string, string>
        {
            ["PUZZLE_LOCAL_POLICY_FILE"] = Path.Combine(root, "custom-policy.json"),
            ["PUZZLE_TEMPLATES_DIR"] = Path.Combine(root, "custom-templates"),
            ["PUZZLE_AUTO_SEND"] = "0"
        };
        var settings = PuzzleSettings.Load((key, fallback) => overrides.GetValueOrDefault(key, fallback), root);
        Assert.Equal(overrides["PUZZLE_LOCAL_POLICY_FILE"], settings.PolicyFile);
        Assert.Equal(overrides["PUZZLE_TEMPLATES_DIR"], settings.TemplateDirectory);
        Assert.False(settings.AutomaticAnswersEnabled);
    }
}
