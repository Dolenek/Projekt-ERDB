using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelOutgoingMatchmaker
    {
        private readonly DuelOfferScanner _offerScanner;
        private readonly DuelAttemptRunner _attemptRunner;
        private readonly DuelOfferClaimManager _claimManager;

        public DuelOutgoingMatchmaker(
            IDuelDiscordClient chatClient,
            DuelOfferScanner offerScanner,
            DuelAttemptRunner attemptRunner)
        {
            if (chatClient == null) throw new ArgumentNullException(nameof(chatClient));
            _offerScanner = offerScanner ?? throw new ArgumentNullException(nameof(offerScanner));
            _attemptRunner = attemptRunner ?? throw new ArgumentNullException(nameof(attemptRunner));
            _claimManager = new DuelOfferClaimManager(chatClient, offerScanner);
        }

        public async Task<DuelRunResult> RunAsync(
            IReadOnlyList<DiscordChannelReference> channels,
            DuelProfileContext profile,
            Func<Task> pauseBotAsync,
            Func<Task> resumeMatchmakingAsync,
            Action<bool> setChallengeState,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var offers = await _offerScanner.ScanAsync(
                profile.Level,
                channels,
                profile.DiscordAuthorId,
                profile.DiscordAuthorName,
                report,
                cancellationToken);
            report?.Invoke($"Collected {offers.Count} eligible cf offer(s) in global FIFO order.");
            foreach (var offer in offers)
            {
                var result = await TryOfferAsync(
                    offer, profile, pauseBotAsync, resumeMatchmakingAsync,
                    setChallengeState, report, cancellationToken);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private async Task<DuelRunResult> TryOfferAsync(
            DuelOffer offer,
            DuelProfileContext profile,
            Func<Task> pauseBotAsync,
            Func<Task> resumeMatchmakingAsync,
            Action<bool> setChallengeState,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var current = await _claimManager.ClaimAsync(offer, profile, report, cancellationToken);
            if (current == null)
            {
                return null;
            }

            var outcome = await RunClaimedOfferAsync(
                current, profile.PlayerName, pauseBotAsync, setChallengeState, report, cancellationToken);
            return await HandleOutcomeAsync(
                outcome, current, resumeMatchmakingAsync, setChallengeState, report, cancellationToken);
        }

        private async Task<DuelAttemptOutcome> RunClaimedOfferAsync(
            DuelOffer offer,
            string playerName,
            Func<Task> pauseBotAsync,
            Action<bool> setChallengeState,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var challengeStarted = false;
            try
            {
                await pauseBotAsync();
                return await _attemptRunner.RunOutgoingAsync(
                    offer, playerName, report,
                    () => { challengeStarted = true; setChallengeState(true); },
                    cancellationToken);
            }
            catch
            {
                if (!challengeStarted)
                {
                    await _claimManager.ReleaseAsync(offer, report, CancellationToken.None);
                }

                throw;
            }
        }

        private async Task<DuelRunResult> HandleOutcomeAsync(
            DuelAttemptOutcome outcome,
            DuelOffer offer,
            Func<Task> resumeMatchmakingAsync,
            Action<bool> setChallengeState,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (outcome == DuelAttemptOutcome.Completed)
            {
                return DuelRunResult.CompletedResult("Duel completed.");
            }

            if (outcome == DuelAttemptOutcome.Uncertain)
            {
                return DuelRunResult.FailedResult("Duel state is uncertain; bot remains stopped.", true);
            }

            if (outcome == DuelAttemptOutcome.FailedSafe)
            {
                setChallengeState(false);
                await _claimManager.ReleaseAsync(offer, report, cancellationToken);
                await resumeMatchmakingAsync();
                return DuelRunResult.FailedResult("Duel stopped: outgoing duel channel did not load.");
            }

            setChallengeState(false);
            if (outcome == DuelAttemptOutcome.Cancelled)
            {
                await _claimManager.MarkAfkAsync(offer, report, cancellationToken);
            }

            await resumeMatchmakingAsync();
            report?.Invoke(outcome == DuelAttemptOutcome.Busy
                ? "Opponent is busy; continuing with the next offer."
                : "Opponent did not accept; continuing with the next offer.");
            return null;
        }

    }
}
