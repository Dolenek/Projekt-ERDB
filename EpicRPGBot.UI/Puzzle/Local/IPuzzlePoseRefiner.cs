using System.Threading;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal interface IPuzzlePoseRefiner
    {
        void Refine(PuzzleTemplateSearch search, PuzzleTemplateLibrary library,
            CancellationToken cancellationToken, int angleStep, int lengthStep,
            double aspectStep, bool retainSeed);
    }
}
