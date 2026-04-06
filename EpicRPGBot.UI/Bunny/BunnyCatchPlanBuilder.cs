using System;

namespace EpicRPGBot.UI.Bunny
{
    public sealed class BunnyCatchPlanBuilder
    {
        public const string FallbackReplyText = "feed feed feed pat pat pat";

        private const int MaxActions = 6;
        private const int GuaranteedPatGain = 8;
        private const int ExpectedPatGain = 10;
        private const int GuaranteedFeedReduction = 18;
        private const int ExpectedFeedReduction = 20;
        private const int CatchThreshold = 85;

        public BunnyCatchPlan Build(BunnyPromptParseResult parseResult)
        {
            if (parseResult == null || !parseResult.IsBunnyPrompt)
            {
                return new BunnyCatchPlan(false, false, string.Empty, false, string.Empty);
            }

            if (!parseResult.HasReadableStats)
            {
                return new BunnyCatchPlan(
                    true,
                    false,
                    FallbackReplyText,
                    true,
                    $"Bunny prompt stats were unreadable; sent fallback '{FallbackReplyText}'.");
            }

            var guaranteedCandidate = FindGuaranteedCandidate(parseResult.Happiness, parseResult.Hunger);
            if (guaranteedCandidate.HasValue)
            {
                var candidate = guaranteedCandidate.Value;
                return new BunnyCatchPlan(
                    true,
                    true,
                    BuildReply(candidate.Feeds, candidate.Pats),
                    false,
                    $"Sent '{BuildReply(candidate.Feeds, candidate.Pats)}' for bunny prompt (happy {parseResult.Happiness}, hunger {parseResult.Hunger}, guaranteed score {candidate.GuaranteedScore}).");
            }

            var heuristicCandidate = FindHeuristicCandidate(parseResult.Happiness, parseResult.Hunger);
            return new BunnyCatchPlan(
                true,
                true,
                BuildReply(heuristicCandidate.Feeds, heuristicCandidate.Pats),
                false,
                $"Sent '{BuildReply(heuristicCandidate.Feeds, heuristicCandidate.Pats)}' for bunny prompt (happy {parseResult.Happiness}, hunger {parseResult.Hunger}, expected score {heuristicCandidate.ExpectedScore}, guaranteed score {heuristicCandidate.GuaranteedScore}).");
        }

        private static ActionCandidate? FindGuaranteedCandidate(int happiness, int hunger)
        {
            ActionCandidate? best = null;
            for (var feeds = 0; feeds <= MaxActions; feeds++)
            {
                for (var pats = 0; pats + feeds <= MaxActions; pats++)
                {
                    if (feeds == 0 && pats == 0)
                    {
                        continue;
                    }

                    var candidate = CreateCandidate(happiness, hunger, feeds, pats);
                    if (candidate.GuaranteedScore < CatchThreshold)
                    {
                        continue;
                    }

                    if (!best.HasValue || CompareGuaranteed(candidate, best.Value) < 0)
                    {
                        best = candidate;
                    }
                }
            }

            return best;
        }

        private static ActionCandidate FindHeuristicCandidate(int happiness, int hunger)
        {
            ActionCandidate? best = null;
            for (var feeds = 0; feeds <= MaxActions; feeds++)
            {
                var pats = MaxActions - feeds;
                var candidate = CreateCandidate(happiness, hunger, feeds, pats);
                if (!best.HasValue || CompareHeuristic(candidate, best.Value) < 0)
                {
                    best = candidate;
                }
            }

            return best ?? CreateCandidate(happiness, hunger, 3, 3);
        }

        private static ActionCandidate CreateCandidate(int happiness, int hunger, int feeds, int pats)
        {
            return new ActionCandidate(
                feeds,
                pats,
                CalculateGuaranteedScore(happiness, hunger, feeds, pats),
                CalculateExpectedScore(happiness, hunger, feeds, pats));
        }

        private static int CalculateGuaranteedScore(int happiness, int hunger, int feeds, int pats)
        {
            return happiness + (GuaranteedPatGain * pats) - Math.Max(0, hunger - (GuaranteedFeedReduction * feeds));
        }

        private static int CalculateExpectedScore(int happiness, int hunger, int feeds, int pats)
        {
            return happiness + (ExpectedPatGain * pats) - Math.Max(0, hunger - (ExpectedFeedReduction * feeds));
        }

        private static int CompareGuaranteed(ActionCandidate left, ActionCandidate right)
        {
            var actionCompare = left.TotalActions.CompareTo(right.TotalActions);
            if (actionCompare != 0)
            {
                return actionCompare;
            }

            var guaranteedCompare = right.GuaranteedScore.CompareTo(left.GuaranteedScore);
            if (guaranteedCompare != 0)
            {
                return guaranteedCompare;
            }

            return right.ExpectedScore.CompareTo(left.ExpectedScore);
        }

        private static int CompareHeuristic(ActionCandidate left, ActionCandidate right)
        {
            var expectedCompare = right.ExpectedScore.CompareTo(left.ExpectedScore);
            if (expectedCompare != 0)
            {
                return expectedCompare;
            }

            var guaranteedCompare = right.GuaranteedScore.CompareTo(left.GuaranteedScore);
            if (guaranteedCompare != 0)
            {
                return guaranteedCompare;
            }

            return right.Feeds.CompareTo(left.Feeds);
        }

        private static string BuildReply(int feeds, int pats)
        {
            var parts = new string[feeds + pats];
            for (var index = 0; index < feeds; index++)
            {
                parts[index] = "feed";
            }

            for (var index = feeds; index < parts.Length; index++)
            {
                parts[index] = "pat";
            }

            return string.Join(" ", parts);
        }

        private struct ActionCandidate
        {
            public ActionCandidate(int feeds, int pats, int guaranteedScore, int expectedScore)
            {
                Feeds = feeds;
                Pats = pats;
                GuaranteedScore = guaranteedScore;
                ExpectedScore = expectedScore;
            }

            public int Feeds { get; }
            public int Pats { get; }
            public int GuaranteedScore { get; }
            public int ExpectedScore { get; }
            public int TotalActions => Feeds + Pats;
        }
    }
}
