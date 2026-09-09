using PuzzleReplay;
using EpicRPGBot.UI.Puzzle;
using EpicRPGBot.UI.Puzzle.Local;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class LocalPuzzleUnsupportedTests
{
    [WindowsFact]
    public async Task LegacyPipeline_RejectsCollectedKeyQuestionCards()
    {
        var root = LocalPuzzleTransformTests.RepositoryRoot();
        var directory = Path.Combine(root, "artifacts/puzzle-dataset-expansion-20260908");
        var records = ReplayDataset.Read(Path.Combine(directory, "unsupported.json"));
        using var provider = new LocalPuzzleAnswerProvider(Path.Combine(root, "Items"),
            PuzzleItemCatalog.Load(Path.Combine(root, "items.json")),
            LocalPuzzlePolicy.Load(Path.Combine(root, "tools/puzzle/legacy-policy.json")));
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
