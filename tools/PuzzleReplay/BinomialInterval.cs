namespace PuzzleReplay
{
    internal sealed class BinomialInterval
    {
        public int Successes { get; set; }
        public int Trials { get; set; }
        public double Estimate { get; set; }
        public double Lower { get; set; }
        public double Upper { get; set; }
        public double ConfidenceLevel => 0.95;
        public string Method => "Wilson score, two-sided, without continuity correction";
    }
}
