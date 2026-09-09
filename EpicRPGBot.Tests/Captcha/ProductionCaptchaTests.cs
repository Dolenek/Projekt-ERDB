using EpicRPGBot.UI.Captcha;
using EpicRPGBot.UI.Captcha.Local;
using Xunit;

namespace EpicRPGBot.Tests.Captcha;

public sealed class ProductionCaptchaTests
{
    [WindowsFact]
    public async Task ShippedPolicy_ValidatesAllSixteenClassesAndAllowsCanonicalKeyAnswer()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        var policy = LocalCaptchaPolicy.Load(Path.Combine(root, "captcha-local.json"));
        Assert.Equal(LocalCaptchaPolicy.FinePipeline, policy.Pipeline);
        Assert.Equal(100, policy.TestTotal);
        Assert.Equal(100, policy.TestCorrect);
        Assert.Equal(0, policy.TestWrong);
        using var provider = new LocalCaptchaAnswerProvider(Path.Combine(root, "Items"),
            CaptchaItemCatalog.Load(Path.Combine(root, "items.json")), policy);
        Assert.Equal(16, provider.Labels.Count);
        Assert.True(provider.AutomaticAnswersValidated);
        Assert.Equal(policy.ValidatedFingerprint, provider.Fingerprint);
        var bytes = File.ReadAllBytes(Path.Combine(root,
            "artifacts/captcha-dataset-expansion-20260908/images/1485124172236456016.png"));
        var result = await provider.SolveAsync(bytes, default);
        Assert.Equal("key", result.Label);
        Assert.True(result.AutomaticSubmissionAllowed);
    }
}
