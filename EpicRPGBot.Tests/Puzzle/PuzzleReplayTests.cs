using System.Text.Json;
using PuzzleReplay;
using EpicRPGBot.UI.Puzzle.Local;
using Xunit;

namespace EpicRPGBot.Tests.Puzzle;

public sealed class PuzzleReplayTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "puzzle-replay-" + Guid.NewGuid());
    public PuzzleReplayTests() { Directory.CreateDirectory(_directory); }

    [Fact]
    public void DuplicateHashes_AreRejectedRegardlessOfCase()
    {
        var first = Record();
        var second = Record();
        second.Sha256 = second.Sha256.ToUpperInvariant();
        var manifest = WriteManifest("duplicates.json", first, second);
        Assert.Throws<InvalidDataException>(() => ReplayDataset.Read(manifest));
    }

    [Fact]
    public void CrossSplitOverlap_IsRejected()
    {
        WriteManifest("calibration.json", Record());
        var holdout = WriteManifest("holdout.json", Record());
        Assert.Throws<InvalidDataException>(() => ReplayDataset.EnsureHoldoutSeparation(holdout, ReplayDataset.Read(holdout)));
    }

    [Fact]
    public void ChecksumMismatch_IsRejected()
    {
        File.WriteAllBytes(Path.Combine(_directory, "image.png"), new byte[] { 1, 2, 3 });
        Assert.Throws<InvalidDataException>(() => ReplayDataset.ReadImage(_directory, Record()));
    }

    [Fact]
    public void MissingExpectedAnswer_IsRejected()
    {
        var record = Record();
        record.Expected = "";
        var manifest = WriteManifest("invalid.json", record);
        Assert.Throws<InvalidDataException>(() => ReplayDataset.Read(manifest));
    }

    [Fact]
    public void ValidationRevokesOldSeal_BeforeAnyNewEvidence()
    {
        var path = Path.Combine(_directory, "policy.json");
        var policy = new LocalPuzzlePolicy { ValidatedFingerprint = "old-seal" };
        ReplayPolicyValidation.Revoke(path, policy);
        Assert.Empty(LocalPuzzlePolicy.Load(path).ValidatedFingerprint);
    }

    [Fact]
    public void AlternatePolicy_DoesNotChangeRepositoryDefault()
    {
        var path = Path.Combine(_directory, "refined.json");
        var options = ReplayOptions.Parse(new[] { _directory, "calibration.json", "results.json", "--policy", path });
        Assert.Equal(path, options.PolicyPath);
        Assert.False(options.Validate);
        Assert.Throws<ArgumentException>(() => ReplayOptions.Parse(new[] { _directory, "calibration.json", "results.json", "--policy" }));
    }

    [Fact]
    public void UnsupportedDiagnostics_CannotEnableValidation()
    {
        var options = ReplayOptions.Parse(new[] { _directory, "unsupported.json", "results.json", "--allow-unsupported" });
        Assert.True(options.AllowUnsupported);
        Assert.False(options.Validate);
        Assert.Throws<ArgumentException>(() => ReplayOptions.Parse(new[]
            { _directory, "unsupported.json", "results.json", "--allow-unsupported", "--validate" }));
    }

    private string WriteManifest(string name, params ReplayRecord[] records)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, JsonSerializer.Serialize(records));
        return path;
    }

    private static ReplayRecord Record() => new()
    {
        Index = 1, Image = "image.png", Expected = "apple", Sha256 = new string('a', 64)
    };

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
