using EpicRPGBot.UI.Captcha;
using EpicRPGBot.UI.Captcha.Local;
using OpenCvSharp;
using Xunit;

namespace EpicRPGBot.Tests.Captcha;

public sealed class AdaptiveCaptchaTests
{
    [WindowsFact]
    public void QuestionLocator_HandlesHorizontalOffsetsAndOmitsText()
    {
        foreach (var textLeft in new[] { 75, 115, 180 })
        {
            using var card = new Mat(85, 700, MatType.CV_8UC3, Scalar.All(34));
            Cv2.PutText(card, "what is this item?", new Point(textLeft, 50),
                HersheyFonts.HersheySimplex, 0.8, Scalar.White, 2);
            var bounds = new CaptchaQuestionRegionLocator().Locate(card);
            Assert.InRange(bounds.Right, textLeft - 10, textLeft);
            Assert.Equal(0, bounds.Left);
            Assert.Equal(5, bounds.Top);
        }
    }

    [WindowsFact]
    public void QuestionLocator_RejectsCardWithoutQuestion()
    {
        using var card = new Mat(200, 500, MatType.CV_8UC3, Scalar.All(34));
        Assert.Throws<ArgumentException>(() => new CaptchaQuestionRegionLocator().Locate(card));
    }

    [WindowsFact]
    public void LineFilter_RemovesThinInterferenceAndPreservesBroadItem()
    {
        using var card = new Mat(100, 100, MatType.CV_8UC3, Scalar.All(34));
        Cv2.Rectangle(card, new Rect(10, 45, 30, 30), new Scalar(30, 30, 220), -1);
        Cv2.Line(card, new Point(50, 10), new Point(90, 10), new Scalar(0, 255, 0), 2);
        using var cleaned = new CaptchaColoredLineFilter().Apply(card);
        Assert.Equal(card.At<Vec3b>(60, 25), cleaned.At<Vec3b>(60, 25));
        Assert.InRange(cleaned.At<Vec3b>(10, 70).Item1, (byte)25, (byte)45);
        Assert.Equal((byte)255, card.At<Vec3b>(10, 70).Item1);
    }

    [WindowsFact]
    public async Task AdaptivePipeline_RecognizesKeyButDoesNotReuseLegacySeal()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        var policy = LocalCaptchaPolicy.Load(Path.Combine(root, "captcha-local.json"));
        policy.Pipeline = LocalCaptchaPolicy.AdaptivePipeline;
        using var provider = new LocalCaptchaAnswerProvider(Path.Combine(root, "Items"),
            CaptchaItemCatalog.Load(Path.Combine(root, "items.json")), policy);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/captcha-dataset-expansion-20260908/images/1485124172236456016.png"));
        var result = await provider.SolveAsync(bytes, default);
        Assert.Equal(16, provider.Labels.Count);
        Assert.Equal("key", result.Label);
        Assert.False(result.AutomaticSubmissionAllowed);
    }

    [WindowsFact]
    public async Task AdaptivePipeline_RecognizesOlderShiftedLayout()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        var policy = LocalCaptchaPolicy.Load(Path.Combine(root, "tools/captcha/adaptive-policy.json"));
        using var provider = new LocalCaptchaAnswerProvider(Path.Combine(root, "Items"),
            CaptchaItemCatalog.Load(Path.Combine(root, "items.json")), policy);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/captcha-training-20260908/images/1196874297508106250.png"));
        var result = await provider.SolveAsync(bytes, default);
        Assert.Equal("ruby", result.Label);
    }
}
