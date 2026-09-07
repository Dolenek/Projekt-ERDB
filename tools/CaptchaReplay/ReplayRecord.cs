using System.Collections.Generic;
using EpicRPGBot.UI.Captcha.Local;

namespace CaptchaReplay
{
    internal sealed class ReplayRecord
    {
        public int Index { get; set; }
        public string MessageId { get; set; }
        public string Image { get; set; }
        public string Sha256 { get; set; }
        public string Expected { get; set; }
        public bool Lines { get; set; }
        public bool Grayscale { get; set; }
    }

    internal sealed class ReplayPrediction
    {
        public int Index { get; set; }
        public string Expected { get; set; }
        public string Predicted { get; set; }
        public bool Accepted { get; set; }
        public bool Correct { get; set; }
        public bool Lines { get; set; }
        public bool Grayscale { get; set; }
        public long Milliseconds { get; set; }
        public IReadOnlyList<CaptchaCandidate> Candidates { get; set; }
        public string Detail { get; set; }
    }
}
