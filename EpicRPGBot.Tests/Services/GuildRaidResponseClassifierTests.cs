using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class GuildRaidResponseClassifierTests
    {
        [Fact]
        public void GuardPrompt_IsDetected()
        {
            var snapshot = CreateSnapshot(
                "m1",
                "EPIC GUARD: stop there, @Firender\nWe have to check you are actually playing",
                "EPIC RPG");

            Assert.True(GuildRaidResponseClassifier.IsGuardPrompt(snapshot));
            Assert.False(GuildRaidResponseClassifier.IsRaidConfirmation(snapshot));
        }

        [Fact]
        public void RaidConfirmation_IsDetectedFromRaidResult()
        {
            var snapshot = CreateSnapshot(
                "m2",
                "EPIC RPG\nRaid\nEnergy\nKNIGHTOFGOOD lost 4951 ENERGY\nYour guild earned 1XP",
                "EPIC RPG");

            Assert.True(GuildRaidResponseClassifier.IsRaidConfirmation(snapshot));
            Assert.False(GuildRaidResponseClassifier.IsGuardPrompt(snapshot));
        }

        [Fact]
        public void Watch_StaysArmedUntilGuardOrConfirmation()
        {
            var watch = new GuildRaidOutcomeWatch();
            watch.Arm();

            var waitingState = watch.Observe(CreateSnapshot("m3", "Firender\nrpg guild raid", "Firender"));
            Assert.Equal(GuildRaidOutcomeState.Waiting, waitingState);
            Assert.True(watch.IsArmed);

            var confirmedState = watch.Observe(CreateSnapshot(
                "m4",
                "EPIC RPG\nRaid\nEnergy\nYour guild earned 1XP",
                "EPIC RPG"));

            Assert.Equal(GuildRaidOutcomeState.Confirmed, confirmedState);
            Assert.False(watch.IsArmed);
        }

        private static DiscordMessageSnapshot CreateSnapshot(string id, string renderedText, string author)
        {
            return new DiscordMessageSnapshot(id, renderedText, author, renderedText);
        }
    }
}
