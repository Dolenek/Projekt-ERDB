using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal interface ICaptchaInterferenceFilter
    {
        // Returns a newly owned image; the caller retains ownership of the input.
        Mat Apply(Mat colors);
    }
}
