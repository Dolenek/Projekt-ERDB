using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Captcha.Local
{
    public sealed class LocalCaptchaAnswerProvider : ICaptchaAnswerProvider, IDisposable
    {
        private readonly ILocalCaptchaRecognizer _recognizer;
        private readonly double _minimumScore;
        private readonly double _minimumMargin;
        private readonly SemaphoreSlim _solveGate = new SemaphoreSlim(1, 1);

        public LocalCaptchaAnswerProvider(string templateDirectory, CaptchaItemCatalog catalog, LocalCaptchaPolicy policy)
            : this(new TemplateCaptchaRecognizer(templateDirectory, catalog,
                (policy ?? throw new ArgumentNullException(nameof(policy))).Pipeline), policy) { }

        // Ownership of the recognizer transfers to this provider.
        public LocalCaptchaAnswerProvider(ILocalCaptchaRecognizer recognizer, LocalCaptchaPolicy policy)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            _recognizer = recognizer ?? throw new ArgumentNullException(nameof(recognizer));
            _minimumScore = policy.MinimumScore;
            _minimumMargin = policy.MinimumMargin;
            AutomaticAnswersValidated = policy.Pipeline == recognizer.Pipeline &&
                policy.AllowsAutomaticAnswers(recognizer.TemplateFingerprint, recognizer.Labels);
            Fingerprint = policy.Fingerprint(recognizer.TemplateFingerprint);
        }
        public bool AutomaticAnswersValidated { get; }
        public string Fingerprint { get; }
        public IReadOnlyList<string> Labels => _recognizer.Labels;
        public string DescribeConfiguration() => "mode=local, items=" + _recognizer.Labels.Count + ", pipeline=" +
            _recognizer.Pipeline + ", validated=" + AutomaticAnswersValidated;

        public async Task<CaptchaAnswerResult> SolveAsync(byte[] imageBytes, CancellationToken cancellationToken)
        {
            await _solveGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try { return await Task.Run(() => Solve(imageBytes, cancellationToken), cancellationToken).ConfigureAwait(false); }
            finally { _solveGate.Release(); }
        }

        private CaptchaAnswerResult Solve(byte[] bytes, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var ranking = _recognizer.Rank(bytes, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return Decide(ranking);
            }
            catch (ArgumentException ex) { return CaptchaAnswerResult.NoMatch("local", ex.Message); }
            catch (OpenCvSharp.OpenCVException ex) { return CaptchaAnswerResult.NoMatch("local", "Image processing failed: " + ex.Message); }
        }

        private CaptchaAnswerResult Decide(IReadOnlyList<CaptchaCandidate> ranking)
        {
            var detail = string.Join("; ", ranking.Select(candidate => string.Format(CultureInfo.InvariantCulture,
                "{0}:score={1:F4},shape={2:F4},color={3:F4}", candidate.Label, candidate.Score, candidate.ShapeScore, candidate.ColorScore)));
            var accepted = ranking.Count >= 2 && ranking.All(candidate =>
                _recognizer.Labels.Contains(candidate.Label) && !double.IsNaN(candidate.Score) &&
                !double.IsInfinity(candidate.Score)) && ranking[0].Score >= _minimumScore &&
                ranking[0].Score - ranking[1].Score >= _minimumMargin;
            return CaptchaAnswerResult.Classified(ranking, accepted, AutomaticAnswersValidated,
                accepted ? detail : "Insufficient similarity or margin. " + detail);
        }

        public void Dispose()
        {
            _recognizer.Dispose();
            _solveGate.Dispose();
        }
    }
}
