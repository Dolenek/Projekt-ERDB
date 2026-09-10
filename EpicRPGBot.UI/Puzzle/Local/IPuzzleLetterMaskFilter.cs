using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal interface IPuzzleLetterMaskFilter
    {
        void Apply(Mat letterMask);
    }
}
