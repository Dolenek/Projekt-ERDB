namespace EpicRPGBot.UI.Puzzle.Local
{
    public sealed class PuzzleCandidate
    {
        public PuzzleCandidate(string label, double score, double shapeScore, double colorScore)
        {
            Label = label;
            Score = score;
            ShapeScore = shapeScore;
            ColorScore = colorScore;
        }
        public string Label { get; }
        public double Score { get; }
        public double ShapeScore { get; }
        public double ColorScore { get; }
    }
}
