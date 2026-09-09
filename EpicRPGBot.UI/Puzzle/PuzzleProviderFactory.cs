using System;
using EpicRPGBot.UI.Puzzle.Local;

namespace EpicRPGBot.UI.Puzzle
{
    public sealed class PuzzleProviderFactory
    {
        public IPuzzleAnswerProvider Create(PuzzleSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new LocalPuzzleAnswerProvider(settings.TemplateDirectory,
                PuzzleItemCatalog.Load(settings.ItemNamesFile), LocalPuzzlePolicy.Load(settings.PolicyFile));
        }
    }
}
