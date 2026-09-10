using System.Linq;
using System.Threading;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzlePoseRefiner : IPuzzlePoseRefiner
    {
        public void Refine(PuzzleTemplateSearch search, PuzzleTemplateLibrary library,
            CancellationToken cancellationToken, int angleStep, int lengthStep,
            double aspectStep, bool retainSeed)
        {
            // Snapshot immutable poses before evaluating disposable transformed variants.
            // Each class, including every competing class, gets the same search budget.
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
