using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleCardPreprocessor
    {
        private readonly IPuzzleInterferenceFilter _interference;
        private readonly IPuzzleQuestionRegionLocator _questionLocator;
        public PuzzleCardPreprocessor() : this(new PuzzleColoredLineFilter()) { }
        public PuzzleCardPreprocessor(IPuzzleInterferenceFilter interference, IPuzzleQuestionRegionLocator questionLocator = null)
        {
            _interference = interference;
            _questionLocator = questionLocator ?? new PuzzleQuestionRegionLocator();
        }

        public Mat Prepare(Mat normalized, bool tall, bool adaptive = false, bool allowNarrowCard = false)
        {
            var bounds = adaptive ? _questionLocator.Locate(normalized, allowNarrowCard) :
                tall ? new Rect(0, 12, 120, 176) : new Rect(0, 5, 65, 75);
            using (var region = new Mat(normalized, bounds))
            using (var cleaned = _interference.Apply(region))
            {
                var reduced = new Mat();
                Cv2.Resize(cleaned, reduced, new Size(), 0.5, 0.5, InterpolationFlags.Area);
                return reduced;
            }
        }
    }
}
