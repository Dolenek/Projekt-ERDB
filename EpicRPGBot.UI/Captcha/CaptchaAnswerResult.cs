using System;
using System.Collections.Generic;
using EpicRPGBot.UI.Captcha.Local;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaAnswerResult
    {
        private CaptchaAnswerResult(string label, bool isMatch, string method, string detail,
            bool automaticSubmissionAllowed = false, IReadOnlyList<CaptchaCandidate> candidates = null)
        {
            Label = label ?? "";
            IsMatch = isMatch;
            Method = string.IsNullOrWhiteSpace(method) ? "unknown" : method;
            Detail = detail ?? "";
            AutomaticSubmissionAllowed = automaticSubmissionAllowed;
            Candidates = candidates ?? Array.Empty<CaptchaCandidate>();
        }
        public string Label { get; }
        public bool IsMatch { get; }
        public string Method { get; }
        public string Detail { get; }
        public bool AutomaticSubmissionAllowed { get; }
        public IReadOnlyList<CaptchaCandidate> Candidates { get; }

        public static CaptchaAnswerResult Classified(IReadOnlyList<CaptchaCandidate> candidates,
            bool accepted, bool validated, string detail)
        {
            return new CaptchaAnswerResult(accepted && candidates.Count > 0 ? candidates[0].Label : "",
                accepted, "local", detail, accepted && validated, candidates);
        }

        public static CaptchaAnswerResult Match(string label, string method, string detail)
        {
            if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Label is required.", nameof(label));
            return new CaptchaAnswerResult(label, true, method, detail);
        }

        public static CaptchaAnswerResult NoMatch(string method, string detail) =>
            new CaptchaAnswerResult("", false, method, detail);
    }
}
