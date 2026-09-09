using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleTemplateMatcher
    {
        public IReadOnlyList<PuzzleCandidate> Rank(PuzzleScene scene,
            PuzzleTemplateLibrary library, CancellationToken cancellationToken, bool refine = false, bool spectral = false,
            bool fine = false)
        {
            using (var search = new PuzzleTemplateSearch(scene, spectral))
            {
                foreach (var variant in library.Variants)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    search.Evaluate(variant);
                }
                if (refine) Refine(search, library, cancellationToken, 7, 2, .15, fine);
                if (fine) Refine(search, library, cancellationToken, 3, 1, .05, false);
                return search.Candidates.OrderByDescending(candidate => candidate.Score).Take(3).ToArray();
            }
        }

        private static void Refine(PuzzleTemplateSearch search, PuzzleTemplateLibrary library,
            CancellationToken cancellationToken, int angleStep, int lengthStep, double aspectStep, bool retainSeed)
        {
            // Every class receives the same refinement budget, including the runner-up.
            foreach (var seed in search.Seeds.ToArray())
                foreach (var angleOffset in new[] { -angleStep, 0, angleStep })
                    foreach (var lengthOffset in new[] { -lengthStep, 0, lengthStep })
                        foreach (var aspectOffset in new[] { -aspectStep, 0, aspectStep })
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (angleOffset == 0 && lengthOffset == 0 && aspectOffset == 0) continue;
                            using (var variant = library.Refine(seed, seed.Angle + angleOffset,
                                seed.Length + lengthOffset, seed.Aspect + aspectOffset))
                                search.Evaluate(variant, retainSeed);
                        }
        }
    }
}
