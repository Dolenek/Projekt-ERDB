using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenCvSharp;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaTemplateMatcher
    {
        public IReadOnlyList<CaptchaCandidate> Rank(CaptchaScene scene,
            CaptchaTemplateLibrary library, CancellationToken cancellationToken)
        {
            var best = new Dictionary<string, CaptchaCandidate>(StringComparer.Ordinal);
            using (var correlationMap = new Mat())
            using (var intersection = new Mat())
            using (var combined = new Mat())
                foreach (var variant in library.Variants)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (variant.Gray.Rows > scene.Gray.Rows || variant.Gray.Cols > scene.Gray.Cols) continue;
                    Cv2.MatchTemplate(scene.Gray, variant.Gray, correlationMap, TemplateMatchModes.CCoeffNormed);
                    Cv2.MatchTemplate(scene.Binary, variant.Binary, intersection, TemplateMatchModes.CCorr);
                    Cv2.AddWeighted(correlationMap, 0.45, intersection,
                        1.1 / (scene.ForegroundArea + variant.ForegroundArea + 1e-8), 0, combined);
                    Cv2.MinMaxLoc(combined, out _, out var shape, out _, out var location);
                    if (double.IsNaN(shape) || double.IsInfinity(shape)) continue;
                    if (best.TryGetValue(variant.Label, out var previous) && shape <= previous.Score) continue;
                    var color = variant.ColorSimilarity(scene.Colors, location);
                    var score = shape * (0.75 + 0.25 * color);
                    if (previous != null && score <= previous.Score) continue;
                    best[variant.Label] = new CaptchaCandidate(variant.Label, score, shape, color);
                }
            return best.Values.OrderByDescending(candidate => candidate.Score).Take(3).ToArray();
        }
    }
}
