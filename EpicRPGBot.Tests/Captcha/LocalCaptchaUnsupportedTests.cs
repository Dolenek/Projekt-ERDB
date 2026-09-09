using CaptchaReplay;
using EpicRPGBot.UI.Captcha;
using EpicRPGBot.UI.Captcha.Local;
using Xunit;

namespace EpicRPGBot.Tests.Captcha;

public sealed class LocalCaptchaUnsupportedTests
{
    [WindowsFact]
    public async Task LegacyPipeline_RejectsCollectedKeyQuestionCards()
    {
        var root = LocalCaptchaTransformTests.RepositoryRoot();
        var directory = Path.Combine(root, "artifacts/captcha-dataset-expansion-20260908");
        var records = ReplayDataset.Read(Path.Combine(directory, "unsupported.json"));
        using var provider = new LocalCaptchaAnswerProvider(Path.Combine(root, "Items"),
            CaptchaItemCatalog.Load(Path.Combine(root, "items.json")),
            LocalCaptchaPolicy.Load(Path.Combine(root, "tools/captcha/legacy-policy.json")));
        Assert.Equal(5, records.Count);
        foreach (var record in records)
        {
            Assert.Equal("key", record.Expected);
            var result = await provider.SolveAsync(ReplayDataset.ReadImage(directory, record), default);
            Assert.False(result.IsMatch, record.MessageId + ": " + result.Detail);
            Assert.False(result.AutomaticSubmissionAllowed);
        }
    }
}
