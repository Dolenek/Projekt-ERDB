using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.TimeCookie
{
    public sealed class TimeCookieLoopStatus
    {
        public TimeCookieLoopStatus(bool isTargetReady, bool hasReadyAutomatedCommands)
        {
            IsTargetReady = isTargetReady;
            HasReadyAutomatedCommands = hasReadyAutomatedCommands;
        }

        public bool IsTargetReady { get; }
        public bool HasReadyAutomatedCommands { get; }
        public bool CanUseTimeCookie => !IsTargetReady && !HasReadyAutomatedCommands;
    }

    public static class TimeCookieLoopEvaluator
    {
        public static TimeCookieLoopStatus Evaluate(
            TimeSpan? targetRemaining,
            TrackedCooldownSnapshot trackedCooldowns,
            bool farmEnabled)
        {
            var snapshot = trackedCooldowns ?? new TrackedCooldownSnapshot(null, null, null, null, null, null, null, null);
            var hasReadyAutomatedCommands =
                IsReady(snapshot.Daily) ||
                IsReady(snapshot.Weekly) ||
                IsReady(snapshot.Hunt) ||
                IsReady(snapshot.Adventure) ||
                IsReady(snapshot.Training) ||
                IsReady(snapshot.Work) ||
                IsReady(snapshot.Lootbox) ||
                (farmEnabled && IsReady(snapshot.Farm));

            return new TimeCookieLoopStatus(IsReady(targetRemaining), hasReadyAutomatedCommands);
        }

        private static bool IsReady(TimeSpan? remaining)
        {
            return !remaining.HasValue || remaining.Value <= TimeSpan.Zero;
        }
    }
}
