using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaTemplateSearch : IDisposable
    {
        private readonly CaptchaScene _scene;
        private readonly Mat _correlation = new Mat();
        private readonly Mat _intersection = new Mat();
        private readonly Mat _combined = new Mat();
        private readonly Dictionary<string, CaptchaCandidate> _best = new Dictionary<string, CaptchaCandidate>(StringComparer.Ordinal);
        private readonly Dictionary<string, CaptchaTemplateVariant> _seeds = new Dictionary<string, CaptchaTemplateVariant>(StringComparer.Ordinal);

        public CaptchaTemplateSearch(CaptchaScene scene) { _scene = scene; }
        public IEnumerable<CaptchaCandidate> Candidates => _best.Values;
        public IEnumerable<CaptchaTemplateVariant> Seeds => _seeds.Values;

        public void Evaluate(CaptchaTemplateVariant variant, bool retainSeed = true)
        {
            if (variant.Gray.Rows > _scene.Gray.Rows || variant.Gray.Cols > _scene.Gray.Cols) return;
            Cv2.MatchTemplate(_scene.Gray, variant.Gray, _correlation, TemplateMatchModes.CCoeffNormed);
            Cv2.MatchTemplate(_scene.Binary, variant.Binary, _intersection, TemplateMatchModes.CCorr);
            Cv2.AddWeighted(_correlation, 0.45, _intersection,
                1.1 / (_scene.ForegroundArea + variant.ForegroundArea + 1e-8), 0, _combined);
            Cv2.MinMaxLoc(_combined, out _, out var shape, out _, out var location);
            if (double.IsNaN(shape) || double.IsInfinity(shape)) return;
            if (_best.TryGetValue(variant.Label, out var previous) && shape <= previous.Score) return;
            var color = variant.ColorSimilarity(_scene.Colors, location);
            var score = shape * (0.75 + 0.25 * color);
            if (previous != null && score <= previous.Score) return;
            _best[variant.Label] = new CaptchaCandidate(variant.Label, score, shape, color);
            if (retainSeed) _seeds[variant.Label] = variant;
        }

        public void Dispose() { _correlation.Dispose(); _intersection.Dispose(); _combined.Dispose(); }
    }
}
