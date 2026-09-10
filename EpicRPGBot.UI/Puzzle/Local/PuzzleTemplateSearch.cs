using System;
using System.Collections.Generic;
using OpenCvSharp;

namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleTemplateSearch : IDisposable
    {
        private readonly PuzzleScene _scene;
        private readonly bool _maximumLuminance;
        private readonly bool _multipleSources;
        private readonly Mat _correlation = new Mat();
        private readonly Mat _intersection = new Mat();
        private readonly Mat _combined = new Mat();
        private readonly Dictionary<string, PuzzleCandidate> _best = new Dictionary<string, PuzzleCandidate>(StringComparer.Ordinal);
        private readonly Dictionary<string, PuzzleTemplatePose> _seeds = new Dictionary<string, PuzzleTemplatePose>(StringComparer.Ordinal);
        private readonly Dictionary<string, double> _sourceScores = new Dictionary<string, double>(StringComparer.Ordinal);

        public PuzzleTemplateSearch(PuzzleScene scene, bool spectral = false, bool multipleSources = false)
        {
            _scene = scene;
            _maximumLuminance = spectral && PuzzleLuminance.IsGrayscale(scene.Colors, scene.Binary);
            _multipleSources = multipleSources;
        }
        public IEnumerable<PuzzleCandidate> Candidates => _best.Values;
        public IEnumerable<PuzzleTemplatePose> Seeds => _seeds.Values;

        public void Evaluate(PuzzleTemplateVariant variant, bool retainSeed = true)
        {
            // Learned grayscale captures cannot explain a colored target.
            if (variant.SourceKey != variant.Label && !_maximumLuminance) return;
            if (variant.Gray.Rows > _scene.Gray.Rows || variant.Gray.Cols > _scene.Gray.Cols) return;
            Cv2.MatchTemplate(_scene.Gray, _maximumLuminance ? variant.MaximumGray : variant.Gray,
                _correlation, TemplateMatchModes.CCoeffNormed);
            Cv2.MatchTemplate(_scene.Binary, variant.Binary, _intersection, TemplateMatchModes.CCorr);
            Cv2.AddWeighted(_correlation, _maximumLuminance ? 0.65 : 0.45, _intersection,
                (_maximumLuminance ? 0.7 : 1.1) / (_scene.ForegroundArea + variant.ForegroundArea + 1e-8), 0, _combined);
            Cv2.MinMaxLoc(_combined, out _, out var shape, out _, out var location);
            if (double.IsNaN(shape) || double.IsInfinity(shape)) return;
            _best.TryGetValue(variant.Label, out var previous);
            var seedKey = _multipleSources ? variant.SourceKey : variant.Label;
            if (_sourceScores.TryGetValue(seedKey, out var previousSourceScore) && shape <= previousSourceScore) return;
            var color = variant.ColorSimilarity(_scene.Colors, location);
            var score = shape * (0.75 + 0.25 * color);
            if (_sourceScores.ContainsKey(seedKey) && score <= previousSourceScore) return;
            _sourceScores[seedKey] = score;
            if (previous == null || score > previous.Score)
                _best[variant.Label] = new PuzzleCandidate(variant.Label, score, shape, color);
            if (retainSeed) _seeds[seedKey] = new PuzzleTemplatePose(variant);
        }

        public void Dispose() { _correlation.Dispose(); _intersection.Dispose(); _combined.Dispose(); }
    }
}
