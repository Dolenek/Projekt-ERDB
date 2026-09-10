using System;
using System.Collections.Generic;
using System.Threading;

namespace EpicRPGBot.UI.Puzzle.Local
{
    public sealed class TemplatePuzzleRecognizer : ILocalPuzzleRecognizer
    {
        private readonly PuzzleTemplateLibrary _library;
        private readonly PuzzleTemplateMatcher _matcher = new PuzzleTemplateMatcher();

        public TemplatePuzzleRecognizer(string directory, PuzzleItemCatalog catalog, string pipeline)
        {
            if (!LocalPuzzlePolicy.IsSupportedPipeline(pipeline))
                throw new ArgumentException("Unsupported local puzzle pipeline.", nameof(pipeline));
            Pipeline = pipeline;
            _library = PuzzleTemplateLibrary.Load(directory, catalog,
                UsesCleanScene, UsesLearnedTemplates, pipeline != LocalPuzzlePolicy.TrainedPipeline);
        }

        public string Pipeline { get; }
        public string TemplateFingerprint => _library.Fingerprint;
        public IReadOnlyList<string> Labels => _library.Labels;
        private bool UsesCleanScene => Pipeline == LocalPuzzlePolicy.CleanPipeline ||
            Pipeline == LocalPuzzlePolicy.AdaptivePipeline || UsesSpectralScoring;
        private bool UsesSpectralScoring => Pipeline == LocalPuzzlePolicy.SpectralPipeline || UsesCrossingFilter;
        private bool UsesCrossingFilter => Pipeline == LocalPuzzlePolicy.CrossingPipeline || UsesLearnedTemplates;
        private bool UsesLearnedTemplates => Pipeline == LocalPuzzlePolicy.TrainedPipeline ||
            Pipeline == LocalPuzzlePolicy.CroppedTrainingPipeline || UsesFineSearch;
        private bool UsesFineSearch => Pipeline == LocalPuzzlePolicy.FinePipeline || UsesGlyphScene;
        private bool UsesGlyphScene => Pipeline == LocalPuzzlePolicy.GlyphPipeline ||
            Pipeline == LocalPuzzlePolicy.MultiSourcePipeline;

        public IReadOnlyList<PuzzleCandidate> Rank(byte[] imageBytes, CancellationToken cancellationToken)
        {
            using (var scene = PuzzleScene.Decode(imageBytes,
                UsesCleanScene, UsesCleanScene && Pipeline != LocalPuzzlePolicy.CleanPipeline,
                UsesCrossingFilter, UsesFineSearch, UsesGlyphScene))
                return _matcher.Rank(scene, _library, cancellationToken,
                    Pipeline == LocalPuzzlePolicy.RefinedPipeline || UsesSpectralScoring, UsesSpectralScoring,
                    UsesFineSearch, Pipeline == LocalPuzzlePolicy.MultiSourcePipeline);
        }

        public void Dispose() => _library.Dispose();
    }
}
