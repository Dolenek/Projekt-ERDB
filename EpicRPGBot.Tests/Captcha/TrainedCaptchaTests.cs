using EpicRPGBot.UI.Captcha;
using EpicRPGBot.UI.Captcha.Local;
using OpenCvSharp;
using Xunit;

namespace EpicRPGBot.Tests.Captcha;

public sealed class TrainedCaptchaTests
{
    [WindowsFact]
    public void CrossingSameColorLines_AreRemovedWithoutErasingBroadItem()
    {
        using var card = new Mat(120, 150, MatType.CV_8UC3, Scalar.All(34));
        Cv2.Rectangle(card, new Rect(100, 70, 35, 35), new Scalar(0, 0, 255), -1);
        Cv2.Line(card, new Point(15, 20), new Point(75, 60), new Scalar(0, 255, 255), 2);
        Cv2.Line(card, new Point(15, 60), new Point(75, 20), new Scalar(0, 255, 255), 2);
        using var cleaned = new CaptchaCrossingLineFilter().Apply(card);
        Assert.InRange(cleaned.At<Vec3b>(40, 45).Item1, (byte)20, (byte)60);
        Assert.Equal(card.At<Vec3b>(85, 115), cleaned.At<Vec3b>(85, 115));
    }

    [WindowsFact]
    public void MaximumChannel_PreservesBrightChromaticStructure()
    {
        using var purple = new Mat(5, 5, MatType.CV_8UC3, new Scalar(255, 0, 128));
        using var maximum = CaptchaLuminance.MaximumChannel(purple);
        using var foreground = CaptchaForeground.CreateMask(purple);
        Assert.Equal((byte)255, maximum.At<byte>(2, 2));
        Assert.False(CaptchaLuminance.IsGrayscale(purple, foreground));
        using var neutral = new Mat(5, 5, MatType.CV_8UC3, Scalar.All(100));
        Assert.True(CaptchaLuminance.IsGrayscale(neutral, foreground));
    }

    [WindowsFact]
    public async Task RootExamples_AreRecognizedWithoutAutomaticSubmission()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        using var provider = Create(root);
        foreach (var example in new[] { ("epic_guard_dragon_scale.webp", "dragon scale"),
                                        ("epic_guard_mermaid.webp", "mermaid hair") })
        {
            var result = await provider.SolveAsync(File.ReadAllBytes(Path.Combine(root, example.Item1)), default);
            Assert.Equal(example.Item2, result.Label);
            Assert.False(result.AutomaticSubmissionAllowed);
        }
    }

    [WindowsFact]
    public async Task GrayscaleEpicCoin_SeparateDevelopmentExampleUsesLearnedTemplate()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        using var provider = Create(root);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/captcha-training-20260908/images/1266401205928595520.png"));
        var result = await provider.SolveAsync(bytes, default);
        Assert.Equal("epic coin", result.Label);
    }

    [WindowsFact]
    public void LearnedTemplates_ChangeFingerprintAndRequireTheirOwnEvidence()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        var catalog = CaptchaItemCatalog.Load(Path.Combine(root, "items.json"));
        using var baseline = new TemplateCaptchaRecognizer(Path.Combine(root, "Items"), catalog, LocalCaptchaPolicy.CrossingPipeline);
        using var trained = new TemplateCaptchaRecognizer(Path.Combine(root, "Items"), catalog, LocalCaptchaPolicy.CroppedTrainingPipeline);
        Assert.NotEqual(baseline.TemplateFingerprint, trained.TemplateFingerprint);
        Assert.Equal(16, trained.Labels.Count);
    }

    private static LocalCaptchaAnswerProvider Create(string root) => new(Path.Combine(root, "Items"),
        CaptchaItemCatalog.Load(Path.Combine(root, "items.json")),
        LocalCaptchaPolicy.Load(Path.Combine(root, "tools/captcha/fine-policy.json")));

    [WindowsFact]
    public async Task NarrowQuestionCrop_IsSupportedButBlankCardsAreRejected()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        using var provider = Create(root);
        var cropped = File.ReadAllBytes(Path.Combine(root,
            "artifacts/captcha-dataset/images/1406306444474585219.png"));
        Assert.Equal("chip", (await provider.SolveAsync(cropped, default)).Label);
        foreach (var width in new[] { 146, 500 })
        {
            using var blank = new Mat(200, width, MatType.CV_8UC3, Scalar.All(34));
            Cv2.ImEncode(".png", blank, out var bytes);
            Assert.False((await provider.SolveAsync(bytes, default)).IsMatch);
        }
    }

    [WindowsFact]
    public async Task ColoredInterference_DoesNotSelectLearnedGrayCoin()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        using var provider = Create(root);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/captcha-dataset-expansion-20260908/images/1448259748289052765.png"));
        Assert.Equal("apple", (await provider.SolveAsync(bytes, default)).Label);
    }
}
