namespace EpicRPGBot.UI.Captcha.Local
{
    public sealed class CaptchaCandidate
    {
        public CaptchaCandidate(string label, double score, double shapeScore, double colorScore)
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
