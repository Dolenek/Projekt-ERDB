using System;
using System.Collections.Generic;
using System.Threading;

namespace EpicRPGBot.UI.Puzzle.Local
{
    public interface ILocalPuzzleRecognizer : IDisposable
    {
        // Identity and labels must remain immutable for the recognizer's lifetime.
        // Calls are serialized by the owning LocalPuzzleAnswerProvider.
        string Pipeline { get; }
        string TemplateFingerprint { get; }
        IReadOnlyList<string> Labels { get; }
        IReadOnlyList<PuzzleCandidate> Rank(byte[] imageBytes, CancellationToken cancellationToken);
    }
}
