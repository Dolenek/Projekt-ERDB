using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleGlyphMaskFilter : IPuzzleLetterMaskFilter
    {
        public void Apply(Mat letterMask)
        {
            using (var components = new Mat())
            using (var statistics = new Mat())
            using (var centers = new Mat())
            {
                var count = Cv2.ConnectedComponentsWithStats(letterMask, components, statistics, centers);
                var retained = new bool[count];
                for (var component = 1; component < count; component++)
                    retained[component] = statistics.At<int>(component, 2) <= 32 &&
                        statistics.At<int>(component, 3) <= 42;
                for (var row = 0; row < letterMask.Rows; row++)
                    for (var column = 0; column < letterMask.Cols; column++)
                        if (!retained[components.At<int>(row, column)])
                            letterMask.Set(row, column, (byte)0);
            }
        }
    }
}
