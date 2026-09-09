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
            _library = CaptchaTemplateLibrary.Load(directory, catalog,
                UsesCleanScene, UsesLearnedTemplates, pipeline != LocalCaptchaPolicy.TrainedPipeline);
        }

        public string Pipeline { get; }
        public string TemplateFingerprint => _library.Fingerprint;
        public IReadOnlyList<string> Labels => _library.Labels;
        private bool UsesCleanScene => Pipeline == LocalCaptchaPolicy.CleanPipeline ||
            Pipeline == LocalCaptchaPolicy.AdaptivePipeline || UsesSpectralScoring;
        private bool UsesSpectralScoring => Pipeline == LocalCaptchaPolicy.SpectralPipeline || UsesCrossingFilter;
        private bool UsesCrossingFilter => Pipeline == LocalCaptchaPolicy.CrossingPipeline || UsesLearnedTemplates;
        private bool UsesLearnedTemplates => Pipeline == LocalCaptchaPolicy.TrainedPipeline ||
            Pipeline == LocalCaptchaPolicy.CroppedTrainingPipeline || Pipeline == LocalCaptchaPolicy.FinePipeline;

        public IReadOnlyList<CaptchaCandidate> Rank(byte[] imageBytes, CancellationToken cancellationToken)
        {
            using (var scene = CaptchaScene.Decode(imageBytes,
                UsesCleanScene, UsesCleanScene && Pipeline != LocalCaptchaPolicy.CleanPipeline,
                UsesCrossingFilter, Pipeline == LocalCaptchaPolicy.FinePipeline))
                return _matcher.Rank(scene, _library, cancellationToken,
                    Pipeline == LocalCaptchaPolicy.RefinedPipeline || UsesSpectralScoring, UsesSpectralScoring,
                    Pipeline == LocalCaptchaPolicy.FinePipeline);
        }

        public void Dispose() => _library.Dispose();
    }
}
