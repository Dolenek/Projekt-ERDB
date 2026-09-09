using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal interface IPuzzleInterferenceFilter
    {
        // Returns a newly owned image; the caller retains ownership of the input.
        Mat Apply(Mat colors);
    }
}
