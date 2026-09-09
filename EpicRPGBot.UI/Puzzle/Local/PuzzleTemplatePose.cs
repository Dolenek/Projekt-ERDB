namespace EpicRPGBot.UI.Puzzle.Local
{
    internal sealed class PuzzleTemplatePose
    {
        public PuzzleTemplatePose(PuzzleTemplateVariant variant)
        {
            Label = variant.Label;
            SourceKey = variant.SourceKey;
            Angle = variant.Angle;
            Length = variant.Length;
            Aspect = variant.Aspect;
        }
        public string Label { get; }
        public string SourceKey { get; }
        public int Angle { get; }
        public int Length { get; }
        public double Aspect { get; }
    }
}
