using System;

namespace EpicRPGBot.UI.Captcha
{
    public sealed class CaptchaAnswerResult
    {
        private CaptchaAnswerResult(string label, bool isMatch, string method, string detail)
        {
            Label = label ?? string.Empty;
            IsMatch = isMatch;
            Method = string.IsNullOrWhiteSpace(method) ? "unknown" : method.Trim();
            Detail = detail ?? string.Empty;
        }

        public string Label { get; }

        public bool IsMatch { get; }

        public string Method { get; }

        public string Detail { get; }

        public static CaptchaAnswerResult Match(string label, string method, string detail)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException("Label is required for a successful captcha match.", nameof(label));
            }

            return new CaptchaAnswerResult(label, true, method, detail);
        }

        public static CaptchaAnswerResult NoMatch(string method, string detail)
        {
            return new CaptchaAnswerResult(string.Empty, false, method, detail);
        }
    }
}
