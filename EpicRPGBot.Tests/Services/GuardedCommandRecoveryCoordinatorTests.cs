using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class GuardedCommandRecoveryCoordinatorTests
    {
        [Fact]
        public async Task ExecuteAsync_FarmGuardClear_DoesNotResendFarmCommand()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            var attempts = 0;
            var recoveryFinished = 0;
            var clearReply = new DiscordMessageSnapshot(
                "clear-1",
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "firendr plants seed in the ground...\n" +
                "55 carrot have grown from the seed");

            var result = await coordinator.ExecuteAsync(
                "rpg farm",
                registerGuard =>
                {
                    attempts++;
                    registerGuard(coordinator.ObserveIncident("guard-1", "rpg farm"));
                    coordinator.CompleteIncident(clearReply);
                    return Task.FromResult(Result("out-1", "guard-1", "EPIC GUARD: stop there,"));
                },
                null,
                () => recoveryFinished++);

            Assert.Equal(1, attempts);
            Assert.Same(clearReply, result.ReplyMessage);
            Assert.Equal(1, recoveryFinished);
        }

        [Fact]
        public void CompleteIncident_WithoutCommand_ReturnsEmptyCommand()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            coordinator.ObserveIncident("guard-1", string.Empty);

            var interruptedCommand = coordinator.CompleteIncident();

            Assert.Equal(string.Empty, interruptedCommand);
        }

        [Fact]
        public async Task Reset_CancelsPendingRecoveryWithoutRetry()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            var registration = coordinator.ObserveIncident("guard-1", "rpg use time cookie");

            coordinator.Reset();

            Assert.False((await registration.Completion).WasCleared);
        }

        [Fact]
        public async Task ExecuteAsync_ResetDuringGuard_ReturnsUnconfirmedWithoutRetry()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            var attempts = 0;

            var result = await coordinator.ExecuteAsync(
                "rpg use time cookie",
                registerGuard =>
                {
                    attempts++;
                    registerGuard(coordinator.ObserveIncident("guard-1", "rpg use time cookie"));
                    coordinator.Reset();
                    return Task.FromResult(Result("out-1", "guard-1", "EPIC GUARD: stop there,"));
                },
                null,
                null);

            Assert.Equal(1, attempts);
            Assert.False(result.IsConfirmed);
        }

        [Fact]
        public async Task ExecuteAsync_ClearWithoutContinuation_DoesNotResend()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            var attempts = 0;

            var result = await coordinator.ExecuteAsync(
                "rpg tr",
                registerGuard =>
                {
                    attempts++;
                    registerGuard(coordinator.ObserveIncident("guard-1", "rpg tr"));
                    coordinator.CompleteIncident();
                    return Task.FromResult(Result("out-1", "guard-1", "EPIC GUARD: stop there,"));
                },
                null,
                null);

            Assert.Equal(1, attempts);
            Assert.Equal("guard-1", result.ReplyMessage?.Id);
        }

        [Fact]
        public async Task ObserveIncident_AttachesCommandToExistingDetection()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            coordinator.ObserveIncident("guard-1", string.Empty);
            var registration = coordinator.ObserveIncident("guard-1", "rpg card hand");

            var interruptedCommand = coordinator.CompleteIncident();

            Assert.Equal("rpg card hand", interruptedCommand);
            Assert.True((await registration.Completion).WasCleared);
        }

        [Fact]
        public async Task ExecuteAsync_TimeCookieGuard_DoesNotResendAndReturnsReduction()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            var attempts = 0;
            var clearReply = new DiscordMessageSnapshot(
                "clear-1",
                "EPIC GUARD: Everything seems fine firendr, keep playing\n" +
                "You ate a time cookie and jumped 12 minute(s) ahead.");

            var result = await coordinator.ExecuteAsync(
                "rpg use time cookie",
                registerGuard =>
                {
                    attempts++;
                    registerGuard(coordinator.ObserveIncident(
                        "guard-1",
                        "rpg use time cookie"));
                    coordinator.CompleteIncident(clearReply);
                    return Task.FromResult(Result(
                        "out-1",
                        "guard-1",
                        "EPIC GUARD: stop there,"));
                },
                null,
                null);

            Assert.Equal(1, attempts);
            Assert.Contains("12 minute(s) ahead", result.ReplyMessage.Text);
        }

        [Fact]
        public void CompleteIncident_DuplicateClear_IsIgnored()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            coordinator.ObserveIncident("guard-1", "rpg hunt h");

            var firstCommand = coordinator.CompleteIncident();
            var duplicateCommand = coordinator.CompleteIncident();

            Assert.Equal("rpg hunt h", firstCommand);
            Assert.Equal(string.Empty, duplicateCommand);
            Assert.Equal(string.Empty, coordinator.InterruptedCommand);
        }

        [Fact]
        public void FindIncidentForCommand_SeesClearProcessedBeforeSendReturns()
        {
            var coordinator = new GuardedCommandRecoveryCoordinator();
            var registration = coordinator.ObserveIncident("guard-1", "rpg tr");
            coordinator.CompleteIncident(new DiscordMessageSnapshot("clear-1", "training"));

            var found = coordinator.FindIncidentForCommand("RPG TR");

            Assert.Same(registration, found);
        }

        private static ConfirmedCommandSendResult Result(
            string outgoingId,
            string replyId,
            string replyText)
        {
            return new ConfirmedCommandSendResult(
                new DiscordMessageSnapshot(outgoingId, "command"),
                new DiscordMessageSnapshot(replyId, replyText),
                1);
        }
    }
}
