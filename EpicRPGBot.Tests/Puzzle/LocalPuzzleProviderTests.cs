using EpicRPGBot.UI.Puzzle.Local;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class LocalPuzzleProviderTests
{
    [Fact]
    public async Task PolicyMutation_CannotChangeValidatedDecisionThresholds()
    {
        var recognizer = new StubRecognizer();
        var policy = ValidPolicy(recognizer);
        using var provider = new LocalPuzzleAnswerProvider(recognizer, policy);
        policy.MinimumScore = 0.1;
        Assert.False((await provider.SolveAsync(new byte[] { 1 }, default)).IsMatch);
    }

    [Fact]
    public async Task PipelineMismatch_CannotReuseValidation()
    {
        var recognizer = new StubRecognizer();
        var policy = ValidPolicy(recognizer);
        recognizer.Pipeline = LocalPuzzlePolicy.RefinedPipeline;
        using var provider = new LocalPuzzleAnswerProvider(recognizer, policy);
        Assert.False(provider.AutomaticAnswersValidated);
        Assert.False((await provider.SolveAsync(new byte[] { 1 }, default)).AutomaticSubmissionAllowed);
    }

    [Theory]
    [InlineData("unknown", 0.99)]
    [InlineData("apple", double.NaN)]
    [InlineData("apple", double.PositiveInfinity)]
    public async Task InvalidCandidate_IsRejected(string label, double score)
    {
        var recognizer = new StubRecognizer { Winner = new PuzzleCandidate(label, score, 1, 1) };
        using var provider = new LocalPuzzleAnswerProvider(recognizer, ValidPolicy(recognizer));
        Assert.False((await provider.SolveAsync(new byte[] { 1 }, default)).IsMatch);
    }

    [Fact]
    public async Task ValidatedCanonicalAnswer_IsEligibleAndRecognizerIsOwned()
    {
        var recognizer = new StubRecognizer { Winner = new PuzzleCandidate("apple", 0.95, 1, 1) };
        using (var provider = new LocalPuzzleAnswerProvider(recognizer, ValidPolicy(recognizer)))
        {
            var result = await provider.SolveAsync(new byte[] { 1 }, default);
            Assert.Equal("apple", result.Label);
            Assert.True(result.AutomaticSubmissionAllowed);
        }
        Assert.True(recognizer.Disposed);
    }

    private static LocalPuzzlePolicy ValidPolicy(StubRecognizer recognizer)
    {
        var policy = new LocalPuzzlePolicy
        {
            MinimumScore = 0.8, MinimumMargin = 0.08, TestTotal = 100, TestCorrect = 90,
            TestClassCounts = new Dictionary<string, int> { ["apple"] = 50, ["coin"] = 50 }
        };
        policy.ValidatedFingerprint = policy.Fingerprint(recognizer.TemplateFingerprint);
        return policy;
    }

    private sealed class StubRecognizer : ILocalPuzzleRecognizer
    {
        public string Pipeline { get; set; } = LocalPuzzlePolicy.CurrentPipeline;
        public string TemplateFingerprint => "test-templates";
        public IReadOnlyList<string> Labels => new[] { "apple", "coin" };
        public PuzzleCandidate Winner { get; set; } = new("apple", 0.7, 0.7, 1);
        public bool Disposed { get; private set; }
        public IReadOnlyList<PuzzleCandidate> Rank(byte[] imageBytes, CancellationToken token)
            => new[] { Winner, new PuzzleCandidate("coin", 0.4, 0.4, 1) };
        public void Dispose() { Disposed = true; }
    }
}
