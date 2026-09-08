using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EpicRPGBot.UI.Captcha.Local
{
    internal sealed class CaptchaTemplateMatcher
    {
        public IReadOnlyList<CaptchaCandidate> Rank(CaptchaScene scene,
            CaptchaTemplateLibrary library, CancellationToken cancellationToken, bool refine = false)
        {
            using (var search = new CaptchaTemplateSearch(scene))
            {
                foreach (var variant in library.Variants)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    search.Evaluate(variant);
                }
                if (refine) Refine(search, library, cancellationToken);
                return search.Candidates.OrderByDescending(candidate => candidate.Score).Take(3).ToArray();
            }
        }

        private static void Refine(CaptchaTemplateSearch search, CaptchaTemplateLibrary library,
            CancellationToken cancellationToken)
        {
            // Every class receives the same refinement budget, including the runner-up.
            foreach (var seed in search.Seeds.ToArray())
                foreach (var angleOffset in new[] { -7, 0, 7 })
                    foreach (var lengthOffset in new[] { -2, 0, 2 })
                        foreach (var aspectOffset in new[] { -0.15, 0, 0.15 })
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (angleOffset == 0 && lengthOffset == 0 && aspectOffset == 0) continue;
                            using (var variant = library.Refine(seed, seed.Angle + angleOffset,
                                seed.Length + lengthOffset, seed.Aspect + aspectOffset))
                                search.Evaluate(variant, retainSeed: false);
                        }
        }
    }
}
