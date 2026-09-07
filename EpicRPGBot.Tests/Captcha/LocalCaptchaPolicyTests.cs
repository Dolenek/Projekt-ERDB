using EpicRPGBot.UI.Captcha.Local;
using Xunit;

namespace EpicRPGBot.Tests.Captcha;

public sealed class LocalCaptchaPolicyTests
{
    [Fact]
    public void UnvalidatedDefaults_NeverAllowAutomaticAnswers()
    {
        Assert.False(new LocalCaptchaPolicy().AllowsAutomaticAnswers("templates", new[] { "apple" }));
    }

    [Fact]
    public void ValidReport_RequiresMatchingTemplatesAndThresholds()
    {
        var policy = ValidPolicy();
        Assert.True(policy.AllowsAutomaticAnswers("templates", new[] { "apple" }));
        Assert.False(policy.AllowsAutomaticAnswers("changed-template", new[] { "apple" }));
        policy.MinimumScore += 0.01;
        Assert.False(policy.AllowsAutomaticAnswers("templates", new[] { "apple" }));
    }

    [Theory]
    [InlineData(100, 89, 0)]
    [InlineData(100, 90, 1)]
    [InlineData(99, 99, 0)]
    [InlineData(100, 101, 0)]
    public void InsufficientResults_DoNotAllowAutomaticAnswers(int total, int correct, int wrong)
    {
        var policy = ValidPolicy();
        policy.TestTotal = total;
        policy.TestCorrect = correct;
        policy.TestWrong = wrong;
        Assert.False(policy.AllowsAutomaticAnswers("templates", new[] { "apple" }));
    }

    [Fact]
    public void MissingClassCoverage_IsNotValidated()
    {
        Assert.False(ValidPolicy().AllowsAutomaticAnswers("templates", new[] { "apple", "coin" }));
    }

    private static LocalCaptchaPolicy ValidPolicy()
    {
        var policy = new LocalCaptchaPolicy
        {
            TestTotal = 100, TestCorrect = 90, TestWrong = 0,
            TestClassCounts = new Dictionary<string, int> { ["apple"] = 100 }
        };
        policy.ValidatedFingerprint = policy.Fingerprint("templates");
        return policy;
    }
}
