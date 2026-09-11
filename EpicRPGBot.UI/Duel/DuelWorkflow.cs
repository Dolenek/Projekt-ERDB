using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelWorkflow
    {
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelChannelCatalog _channelCatalog;
        private readonly DuelProfileLoader _profileLoader;
        private readonly DuelOutgoingMatchmaker _outgoingMatchmaker;
        private readonly DuelIncomingMatchmaker _incomingMatchmaker;
        private bool _challengeMayBeActive;

        public DuelWorkflow(
            IDuelDiscordClient duelChatClient,
            IDiscordChatClient profileChatClient,
            ConfirmedCommandSender profileCommandSender,
            AppSettingsService settingsService)
        {
            _chatClient = duelChatClient ?? throw new ArgumentNullException(nameof(duelChatClient));
            _channelCatalog = new DuelChannelCatalog();
            var offerParser = new DuelOfferParser();
            var messageParser = new DuelMessageParser();
            _profileLoader = new DuelProfileLoader(profileChatClient, profileCommandSender, settingsService);
            var offerScanner = new DuelOfferScanner(duelChatClient, _channelCatalog, offerParser);
            var attemptRunner = new DuelAttemptRunner(duelChatClient, messageParser, new DuelWeaponSelector());
            _outgoingMatchmaker = new DuelOutgoingMatchmaker(duelChatClient, offerScanner, attemptRunner);
            _incomingMatchmaker = new DuelIncomingMatchmaker(
                duelChatClient,
                _channelCatalog,
                new DuelIncomingWatcher(duelChatClient, messageParser),
                new DuelIncomingAttemptRunner(duelChatClient, messageParser));
        }

        public async Task<DuelRunResult> RunAsync(
            Action<string> report,
            Func<Task> pauseBotAsync,
            Func<Task> resumeMatchmakingAsync,
            CancellationToken cancellationToken)
        {
            _challengeMayBeActive = false;
            _incomingMatchmaker.Reset();
            try
            {
                return await RunCoreAsync(report, pauseBotAsync, resumeMatchmakingAsync, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                await CleanupSafeCancellationAsync(report);
                return DuelRunResult.CancelledResult(_challengeMayBeActive);
            }
            catch (Exception ex)
            {
                await CleanupSafeCancellationAsync(report);
                return DuelRunResult.FailedResult("Duel stopped: " + ex.Message, _challengeMayBeActive);
            }
        }

        private async Task<DuelRunResult> RunCoreAsync(
            Action<string> report,
            Func<Task> pauseBotAsync,
            Func<Task> resumeMatchmakingAsync,
            CancellationToken cancellationToken)
        {
            var profile = await _profileLoader.LoadAsync(report, cancellationToken);
            if (profile == null)
            {
                return DuelRunResult.FailedResult("Duel stopped: profile name or level could not be resolved.");
            }

            report?.Invoke("Loading and validating the fixed duel channel catalog.");
            var channels = LoadAndValidateChannels(profile.Level);
            var outgoingResult = await _outgoingMatchmaker.RunAsync(
                channels,
                profile,
                pauseBotAsync,
                resumeMatchmakingAsync,
                SetChallengeState,
                report,
                cancellationToken);
            return outgoingResult ?? await _incomingMatchmaker.RunAsync(
                channels,
                profile,
                pauseBotAsync,
                resumeMatchmakingAsync,
                SetChallengeState,
                report,
                cancellationToken);
        }

        private IReadOnlyList<DiscordChannelReference> LoadAndValidateChannels(int playerLevel)
        {
            var channels = _channelCatalog.MergeFixedChannels(Array.Empty<DiscordChannelReference>());
            _channelCatalog.ResolveRelevantListingChannels(playerLevel, channels);
            _channelCatalog.ResolvePlayerListingChannel(playerLevel, channels);
            _channelCatalog.ResolveDuelingChannels(channels);
            _channelCatalog.ResolveOutgoingDuelChannel(channels);
            return channels;
        }

        private async Task CleanupSafeCancellationAsync(Action<string> report)
        {
            if (_challengeMayBeActive)
            {
                return;
            }

            try
            {
                await _incomingMatchmaker.CleanupOwnListingAsync(report);
            }
            catch (Exception ex)
            {
                report?.Invoke("Own cf offer cleanup failed: " + ex.Message);
            }
        }

        private void SetChallengeState(bool mayBeActive)
        {
            _challengeMayBeActive = mayBeActive;
        }
    }
}
