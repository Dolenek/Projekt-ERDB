using EpicRPGBot.UI.Puzzle;
using EpicRPGBot.UI.Puzzle.Local;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.Tests.Puzzle;

internal sealed class SolverHarness
{
    public SolverHarness(bool accepted = true, bool validated = true, bool enabled = true)
    {
        Provider.Result = PuzzleAnswerResult.Classified(new[]
        {
            new PuzzleCandidate("apple", 0.95, 0.95, 1),
            new PuzzleCandidate("ruby", 0.6, 0.6, 1)
        }, accepted, validated, "test");
        Solver = new PuzzleSolverService(Images, () => Provider, () => enabled);
    }
    public FakeImages Images { get; } = new();
    public FakeProvider Provider { get; } = new();
    public PuzzleSolverService Solver { get; }
    public List<string> Sent { get; } = new();
    public bool Paused { get; private set; }
    public bool IncidentActive { get; set; } = true;

    public Task Solve() => Solver.TrySolveAsync("guard", "guard", "previous", (answer, token) =>
    {
        token.ThrowIfCancellationRequested();
        Sent.Add(answer);
        return Task.FromResult(true);
    }, () => Paused = true, () => IncidentActive, _ => { });
}

internal sealed class FakeImages : IPuzzleImageSource
{
    public byte[]? Bytes { get; set; } = new byte[] { 1, 2, 3 };
    public Task<byte[]> LoadAsync(string targetId, string adjacentId,
        Action<string> report, CancellationToken cancellationToken) => Task.FromResult(Bytes!);
}

internal sealed class FakeProvider : IPuzzleAnswerProvider
{
    public PuzzleAnswerResult Result { get; set; } = null!;
    public bool WaitForRelease { get; set; }
    public bool Fail { get; set; }
    public int CallCount { get; private set; }
    public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public string DescribeConfiguration() => "fake";
    public async Task<PuzzleAnswerResult> SolveAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        CallCount++;
        Entered.TrySetResult(true);
        if (WaitForRelease) await Release.Task.WaitAsync(cancellationToken);
        if (Fail) throw new InvalidOperationException("Decode failed");
        return Result;
    }
}
