using System;
using System.Collections.Generic;
using System.Threading;

namespace EpicRPGBot.UI.Captcha.Local
{
    public interface ILocalCaptchaRecognizer : IDisposable
    {
        // Identity and labels must remain immutable for the recognizer's lifetime.
        // Calls are serialized by the owning LocalCaptchaAnswerProvider.
        string Pipeline { get; }
        string TemplateFingerprint { get; }
        IReadOnlyList<string> Labels { get; }
        IReadOnlyList<CaptchaCandidate> Rank(byte[] imageBytes, CancellationToken cancellationToken);
    }
}
