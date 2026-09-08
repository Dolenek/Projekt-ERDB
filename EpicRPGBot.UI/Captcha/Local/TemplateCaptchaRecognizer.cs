using System;
using System.Collections.Generic;
using System.Threading;

namespace EpicRPGBot.UI.Captcha.Local
{
    public sealed class TemplateCaptchaRecognizer : ILocalCaptchaRecognizer
    {
        private readonly CaptchaTemplateLibrary _library;
        private readonly CaptchaTemplateMatcher _matcher = new CaptchaTemplateMatcher();

        public TemplateCaptchaRecognizer(string directory, CaptchaItemCatalog catalog, string pipeline)
        {
            if (!LocalCaptchaPolicy.IsSupportedPipeline(pipeline))
                throw new ArgumentException("Unsupported local captcha pipeline.", nameof(pipeline));
            Pipeline = pipeline;
            _library = CaptchaTemplateLibrary.Load(directory, catalog);
        }

        public string Pipeline { get; }
        public string TemplateFingerprint => _library.Fingerprint;
        public IReadOnlyList<string> Labels => Array.AsReadOnly(CaptchaTemplateLibrary.SupportedLabels);

        public IReadOnlyList<CaptchaCandidate> Rank(byte[] imageBytes, CancellationToken cancellationToken)
        {
            using (var scene = CaptchaScene.Decode(imageBytes))
                return _matcher.Rank(scene, _library, cancellationToken,
                    Pipeline == LocalCaptchaPolicy.RefinedPipeline);
        }

        public void Dispose() => _library.Dispose();
    }
}
