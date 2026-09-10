using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal interface IPuzzleQuestionRegionLocator
    {
        Rect Locate(Mat colors, bool allowNarrowCard = false);
    }
}
