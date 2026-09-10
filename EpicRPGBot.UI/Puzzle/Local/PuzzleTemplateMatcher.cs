using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleTemplateMatcher
    {
        private readonly IPuzzlePoseRefiner _refiner;

        public PuzzleTemplateMatcher(IPuzzlePoseRefiner refiner = null)
        {
            _refiner = refiner ?? new PuzzlePoseRefiner();
        }

        public IReadOnlyList<PuzzleCandidate> Rank(PuzzleScene scene,
            PuzzleTemplateLibrary library, CancellationToken cancellationToken, bool refine = false, bool spectral = false,
            bool fine = false, bool multipleSources = false)
        {
            using (var search = new PuzzleTemplateSearch(scene, spectral, multipleSources))
            {
                foreach (var variant in library.Variants)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    search.Evaluate(variant);
                }
                if (refine) _refiner.Refine(search, library, cancellationToken, 7, 2, .15, fine);
                if (fine) _refiner.Refine(search, library, cancellationToken, 3, 1, .05, false);
                return search.Candidates.OrderByDescending(candidate => candidate.Score).Take(3).ToArray();
            }
        }

    }
}
