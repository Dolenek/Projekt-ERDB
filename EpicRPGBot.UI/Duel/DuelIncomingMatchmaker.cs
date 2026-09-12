using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelIncomingMatchmaker
    {
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelChannelCatalog _channelCatalog;
        private readonly DuelIncomingWatcher _incomingWatcher;
        private readonly DuelIncomingAttemptRunner _attemptRunner;
        private DiscordChannelReference _ownListingChannel;
        private DiscordMessageSnapshot _ownListingMessage;

        public DuelIncomingMatchmaker(
            IDuelDiscordClient chatClient,
            DuelChannelCatalog channelCatalog,
            DuelIncomingWatcher incomingWatcher,
            DuelIncomingAttemptRunner attemptRunner)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _channelCatalog = channelCatalog ?? throw new ArgumentNullException(nameof(channelCatalog));
            _incomingWatcher = incomingWatcher ?? throw new ArgumentNullException(nameof(incomingWatcher));
            _attemptRunner = attemptRunner ?? throw new ArgumentNullException(nameof(attemptRunner));
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
            await resumeMatchmakingAsync();
            var duelChannels = _channelCatalog.ResolveDuelingChannels(channels);
            var requestCutoffUtc = DateTimeOffset.UtcNow;
            var mentionBaseline = await _incomingWatcher.CaptureMentionBaselineAsync(
                duelChannels,
                cancellationToken);
            report?.Invoke("Mention badge baseline: " + string.Join(", ", duelChannels.Select(channel =>
                $"#{channel.Name}={mentionBaseline[channel.Id]}")) + ".");
            await PostOwnOfferAsync(channels, profile.Level, report, cancellationToken);
            report?.Invoke("Waiting in the listing channel for a new mention badge from #dueling-1 through #dueling-4.");
            var processed = new HashSet<string>(StringComparer.Ordinal);
            while (true)
            {
                var request = await _incomingWatcher.WaitForRequestAsync(
                    duelChannels,
                    _ownListingChannel,
                    mentionBaseline,
                    profile.PlayerName,
                    requestCutoffUtc,
                    processed,
                    report,
                    cancellationToken);
                await pauseBotAsync();
                setChallengeState(true);
                var outcome = await _attemptRunner.AcceptAndWaitAsync(
                    request,
                    profile.PlayerName,
                    report,
                    cancellationToken);
                var result = HandleTerminalOutcome(outcome);
                if (result != null)
                {
                    return result;
                }

                setChallengeState(false);
                await resumeMatchmakingAsync();
                report?.Invoke("Incoming duel was cancelled; continuing to wait.");
                await ParkAtOwnListingAsync(cancellationToken);
            }
        }

        public async Task CleanupOwnListingAsync(Action<string> report)
        {
            if (_ownListingMessage == null || _ownListingChannel == null)
            {
                return;
            }

            if (!await _chatClient.NavigateToChannelAndWaitAsync(
                    _ownListingChannel.Url,
                    CancellationToken.None))
            {
                report?.Invoke("Own cf offer cleanup failed because its listing channel did not load.");
                return;
            }

            var cutoff = _ownListingMessage.CreatedAtUtc?.AddSeconds(-1) ??
                         DateTimeOffset.UtcNow - TimeSpan.FromMinutes(15);
            var messages = await _chatClient.GetMessagesSinceAsync(cutoff, CancellationToken.None);
            if (!messages.Any(message => message.Id == _ownListingMessage.Id))
            {
                report?.Invoke("Own cf offer is already absent; no deletion is needed.");
                return;
            }

            var deleted = await _chatClient.DeleteOwnMessageAsync(_ownListingMessage.Id, CancellationToken.None);
            report?.Invoke(deleted ? "Own cf offer cleanup completed." : "Own cf offer could not be deleted.");
        }

        public void Reset()
        {
            _ownListingChannel = null;
            _ownListingMessage = null;
        }

        private async Task PostOwnOfferAsync(
            IReadOnlyList<DiscordChannelReference> channels,
            int playerLevel,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            _ownListingChannel = _channelCatalog.ResolvePlayerListingChannel(playerLevel, channels);
            if (!await _chatClient.NavigateToChannelAndWaitAsync(_ownListingChannel.Url, cancellationToken))
            {
                throw new InvalidOperationException($"Duel channel '#{_ownListingChannel.Name}' did not load.");
            }

            _ownListingMessage = await _chatClient.SendMessageAndWaitForOutgoingAsync(
                $"{playerLevel} cf",
                cancellationToken);
            if (_ownListingMessage == null)
            {
                throw new InvalidOperationException("Own cf offer could not be registered.");
            }

            report?.Invoke($"Posted '{playerLevel} cf' in #{_ownListingChannel.Name}; waiting for a duel.");
        }

        private async Task ParkAtOwnListingAsync(CancellationToken cancellationToken)
        {
            if (_ownListingChannel == null ||
                !await _chatClient.NavigateToChannelAndWaitAsync(_ownListingChannel.Url, cancellationToken))
            {
                throw new InvalidOperationException("Could not return to the own cf listing channel.");
            }
        }

        private static DuelRunResult HandleTerminalOutcome(DuelAttemptOutcome outcome)
        {
            if (outcome == DuelAttemptOutcome.Completed)
            {
                return DuelRunResult.CompletedResult("Duel completed.");
            }

            return outcome == DuelAttemptOutcome.Uncertain
                ? DuelRunResult.FailedResult("Duel state is uncertain; bot remains stopped.", true)
                : outcome == DuelAttemptOutcome.FailedSafe
                    ? DuelRunResult.FailedResult("Incoming duel state is uncertain; bot remains stopped.", true)
                    : null;
        }
    }
}
