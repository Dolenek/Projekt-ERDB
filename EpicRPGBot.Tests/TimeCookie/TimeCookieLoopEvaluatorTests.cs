using System;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.TimeCookie;
using Xunit;

namespace EpicRPGBot.Tests.TimeCookie
{
    public sealed class TimeCookieLoopEvaluatorTests
    {
        [Fact]
        public void Evaluate_TargetAlreadyReady_DoesNotAllowAnotherCookie()
        {
            var status = TimeCookieLoopEvaluator.Evaluate(
                null,
                CreateTrackedSnapshot(
                    hunt: TimeSpan.FromMinutes(1),
                    adventure: TimeSpan.FromMinutes(2),
                    training: TimeSpan.FromMinutes(3),
                    work: TimeSpan.FromMinutes(4),
                    farm: TimeSpan.FromMinutes(5),
                    lootbox: TimeSpan.FromMinutes(6)),
                farmEnabled: true);

            Assert.True(status.IsTargetReady);
            Assert.False(status.CanUseTimeCookie);
        }

        [Fact]
        public void Evaluate_TargetNotReadyAndAutomatedCooldownsBusy_AllowsCookie()
        {
            var status = TimeCookieLoopEvaluator.Evaluate(
                TimeSpan.FromMinutes(10),
                CreateTrackedSnapshot(
                    hunt: TimeSpan.FromMinutes(1),
                    adventure: TimeSpan.FromMinutes(2),
                    training: TimeSpan.FromMinutes(3),
                    work: TimeSpan.FromMinutes(4),
                    farm: TimeSpan.FromMinutes(5),
                    lootbox: TimeSpan.FromMinutes(6)),
                farmEnabled: true);

            Assert.False(status.IsTargetReady);
            Assert.False(status.HasReadyAutomatedCommands);
            Assert.True(status.CanUseTimeCookie);
        }

        [Fact]
        public void Evaluate_TargetReadyWhileAutomatedCommandsAreReady_StopsAfterBatch()
        {
            var status = TimeCookieLoopEvaluator.Evaluate(
                null,
                CreateTrackedSnapshot(
                    hunt: null,
                    adventure: TimeSpan.FromMinutes(2),
                    training: TimeSpan.FromMinutes(3),
                    work: TimeSpan.FromMinutes(4),
                    farm: TimeSpan.FromMinutes(5),
                    lootbox: TimeSpan.FromMinutes(6)),
                farmEnabled: true);

            Assert.True(status.IsTargetReady);
            Assert.True(status.HasReadyAutomatedCommands);
            Assert.False(status.CanUseTimeCookie);
        }

        [Fact]
        public void Evaluate_FarmReadyWhenFarmDisabled_IgnoresFarmForAutoBatch()
        {
            var status = TimeCookieLoopEvaluator.Evaluate(
                TimeSpan.FromMinutes(10),
                CreateTrackedSnapshot(
                    hunt: TimeSpan.FromMinutes(1),
                    adventure: TimeSpan.FromMinutes(2),
                    training: TimeSpan.FromMinutes(3),
                    work: TimeSpan.FromMinutes(4),
                    farm: null,
                    lootbox: TimeSpan.FromMinutes(6)),
                farmEnabled: false);

            Assert.False(status.HasReadyAutomatedCommands);
            Assert.True(status.CanUseTimeCookie);
        }

        private static TrackedCooldownSnapshot CreateTrackedSnapshot(
            TimeSpan? hunt,
            TimeSpan? adventure,
            TimeSpan? training,
            TimeSpan? work,
            TimeSpan? farm,
            TimeSpan? lootbox)
        {
            return new TrackedCooldownSnapshot(hunt, adventure, training, work, farm, lootbox);
        }
    }
}
