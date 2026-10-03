namespace EpicRPGBot.Tests.Puzzle;

public sealed class LocalPuzzleDatasetFactAttribute : WindowsFactAttribute
{
    public LocalPuzzleDatasetFactAttribute(string relativePath)
    {
        if (Skip != null) return;
        var fixturePath = Path.Combine(LocalPuzzleTransformTests.RepositoryRoot(), relativePath);
        if (!File.Exists(fixturePath)) Skip = "Optional local puzzle dataset is missing: " + relativePath;
    }
}
