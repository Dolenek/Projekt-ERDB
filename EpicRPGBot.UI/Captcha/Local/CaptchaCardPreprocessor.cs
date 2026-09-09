using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaCardPreprocessor
    {
        private readonly ICaptchaInterferenceFilter _interference;
        public CaptchaCardPreprocessor() : this(new CaptchaColoredLineFilter()) { }
        public CaptchaCardPreprocessor(ICaptchaInterferenceFilter interference) { _interference = interference; }

        public Mat Prepare(Mat normalized, bool tall, bool adaptive = false, bool allowNarrowCard = false)
        {
            var bounds = adaptive ? new CaptchaQuestionRegionLocator().Locate(normalized, allowNarrowCard) :
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
