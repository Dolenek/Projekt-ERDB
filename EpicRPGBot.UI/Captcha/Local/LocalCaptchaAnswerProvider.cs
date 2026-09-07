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
        private readonly CaptchaTemplateLibrary _library;
        private readonly LocalCaptchaPolicy _policy;
        private readonly SemaphoreSlim _solveGate = new SemaphoreSlim(1, 1);
        private readonly CaptchaTemplateMatcher _matcher = new CaptchaTemplateMatcher();

        public LocalCaptchaAnswerProvider(string templateDirectory, CaptchaItemCatalog catalog, LocalCaptchaPolicy policy)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _library = CaptchaTemplateLibrary.Load(templateDirectory, catalog);
            AutomaticAnswersValidated = policy.AllowsAutomaticAnswers(_library.Fingerprint, CaptchaTemplateLibrary.SupportedLabels);
            Fingerprint = policy.Fingerprint(_library.Fingerprint);
        }
        public bool AutomaticAnswersValidated { get; }
        public string Fingerprint { get; }
        public string DescribeConfiguration() => "mode=local, items=15, pipeline=" +
            LocalCaptchaPolicy.CurrentPipeline + ", validated=" + AutomaticAnswersValidated;

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
                using (var scene = CaptchaScene.Decode(bytes))
                {
                    var ranking = _matcher.Rank(scene, _library, cancellationToken);
                    return Decide(ranking);
                }
            }
            catch (ArgumentException ex) { return CaptchaAnswerResult.NoMatch("local", ex.Message); }
            catch (OpenCvSharp.OpenCVException ex) { return CaptchaAnswerResult.NoMatch("local", "Image processing failed: " + ex.Message); }
        }

        private CaptchaAnswerResult Decide(IReadOnlyList<CaptchaCandidate> ranking)
        {
            var detail = string.Join("; ", ranking.Select(candidate => string.Format(CultureInfo.InvariantCulture,
                "{0}:score={1:F4},shape={2:F4},color={3:F4}", candidate.Label, candidate.Score, candidate.ShapeScore, candidate.ColorScore)));
            var accepted = ranking.Count >= 2 && ranking[0].Score >= _policy.MinimumScore &&
                ranking[0].Score - ranking[1].Score >= _policy.MinimumMargin;
            return CaptchaAnswerResult.Classified(ranking, accepted, AutomaticAnswersValidated,
                accepted ? detail : "Insufficient similarity or margin. " + detail);
        }

        public void Dispose()
        {
            _library.Dispose();
            _solveGate.Dispose();
        }
    }
}
