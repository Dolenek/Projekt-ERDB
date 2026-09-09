using System;
using System.Collections.Generic;
using EpicRPGBot.UI.Puzzle.Local;

namespace EpicRPGBot.UI.Puzzle
{
    public sealed class PuzzleAnswerResult
    {
        private PuzzleAnswerResult(string label, bool isMatch, string method, string detail,
            bool automaticSubmissionAllowed = false, IReadOnlyList<PuzzleCandidate> candidates = null)
        {
            Label = label ?? "";
            IsMatch = isMatch;
            Method = string.IsNullOrWhiteSpace(method) ? "unknown" : method;
            Detail = detail ?? "";
            AutomaticSubmissionAllowed = automaticSubmissionAllowed;
            Candidates = candidates ?? Array.Empty<PuzzleCandidate>();
        }
        public string Label { get; }
        public bool IsMatch { get; }
        public string Method { get; }
        public string Detail { get; }
        public bool AutomaticSubmissionAllowed { get; }
        public IReadOnlyList<PuzzleCandidate> Candidates { get; }

        public static PuzzleAnswerResult Classified(IReadOnlyList<PuzzleCandidate> candidates,
            bool accepted, bool validated, string detail)
        {
            return new PuzzleAnswerResult(accepted && candidates.Count > 0 ? candidates[0].Label : "",
                accepted, "local", detail, accepted && validated, candidates);
        }

        public static PuzzleAnswerResult Match(string label, string method, string detail)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label is required.", nameof(label));
            return new PuzzleAnswerResult(label, true, method, detail);
        }

        public static PuzzleAnswerResult NoMatch(string method, string detail) =>
            new PuzzleAnswerResult("", false, method, detail);
    }
}
