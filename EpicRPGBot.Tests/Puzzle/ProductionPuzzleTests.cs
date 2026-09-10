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
        var result = await provider.SolveAsync(CreateKeyCard(settings.TemplateDirectory), default);
        Assert.Equal("key", result.Label);
        Assert.True(result.AutomaticSubmissionAllowed);
    }

    private static byte[] CreateKeyCard(string templateDirectory)
    {
        using var original = PuzzleTemplateTransforms.ReadTemplate(Path.Combine(templateDirectory, "key.webp"));
        using var icon = PuzzleTemplateTransforms.Transform(original, 0, 48, 1);
        using var card = new OpenCvSharp.Mat(200, 160, OpenCvSharp.MatType.CV_8UC3, OpenCvSharp.Scalar.All(34));
        using (var region = new OpenCvSharp.Mat(card, new OpenCvSharp.Rect(35, 75, icon.Width, icon.Height)))
            icon.CopyTo(region);
        OpenCvSharp.Cv2.ImEncode(".png", card, out var bytes);
        return bytes;
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
