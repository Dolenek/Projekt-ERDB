using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    public sealed class DuelOfferScanner
    {
        private static readonly TimeSpan OfferLifetime = TimeSpan.FromMinutes(15);
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelChannelCatalog _channelCatalog;
        private readonly DuelOfferParser _offerParser;
        private readonly Func<DateTimeOffset> _utcNow;

        public DuelOfferScanner(
            IDuelDiscordClient chatClient,
            DuelChannelCatalog channelCatalog,
            DuelOfferParser offerParser,
            Func<DateTimeOffset> utcNow = null)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _channelCatalog = channelCatalog ?? throw new ArgumentNullException(nameof(channelCatalog));
            _offerParser = offerParser ?? throw new ArgumentNullException(nameof(offerParser));
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        public async Task<IReadOnlyList<DuelOffer>> ScanAsync(
            int playerLevel,
            IReadOnlyList<DiscordChannelReference> discoveredChannels,
            string selfAuthorId,
            string selfAuthorName,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var offers = new List<DuelOffer>();
            var listingChannels = _channelCatalog.ResolveRelevantListingChannels(playerLevel, discoveredChannels);
            foreach (var channel in listingChannels)
            {
                cancellationToken.ThrowIfCancellationRequested();
                report?.Invoke($"Scanning #{channel.Name}.");
                offers.AddRange(await LoadChannelOffersAsync(
                    channel,
                    playerLevel,
                    selfAuthorId,
                    selfAuthorName,
                    report,
                    cancellationToken));
            }

            return offers
                .OrderBy(offer => offer.Message.CreatedAtUtc)
                .ThenBy(offer => offer.Message.Id, StringComparer.Ordinal)
                .ToArray();
        }

        public async Task<DuelOffer> RevalidateAsync(
            DuelOffer offer,
            int playerLevel,
            string selfAuthorId,
            string selfAuthorName,
            CancellationToken cancellationToken)
        {
            if (!await _chatClient.NavigateToChannelAndWaitAsync(offer.Channel.Url, cancellationToken))
            {
                return null;
            }

            var cutoff = _utcNow() - OfferLifetime;
            var messages = await _chatClient.GetMessagesSinceAsync(cutoff, cancellationToken);
            var current = messages.FirstOrDefault(message => message.Id == offer.Message.Id);
            return _offerParser.TryParseEligibleCf(
                current,
                playerLevel,
                cutoff,
                selfAuthorId,
                selfAuthorName,
                out var currentLevel)
                ? new DuelOffer(offer.Channel, current, currentLevel)
                : null;
        }

        private async Task<IReadOnlyList<DuelOffer>> LoadChannelOffersAsync(
            DiscordChannelReference channel,
            int playerLevel,
            string selfAuthorId,
            string selfAuthorName,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (!await _chatClient.NavigateToChannelAndWaitAsync(channel.Url, cancellationToken))
            {
                throw new InvalidOperationException($"Duel channel '#{channel.Name}' did not load.");
            }

            var cutoff = _utcNow() - OfferLifetime;
            var messages = await _chatClient.GetMessagesSinceAsync(cutoff, cancellationToken);
            return ParseOffers(
                channel,
                messages,
                playerLevel,
                cutoff,
                selfAuthorId,
                selfAuthorName,
                report);
        }

        private IReadOnlyList<DuelOffer> ParseOffers(
            DiscordChannelReference channel,
            IReadOnlyList<DiscordMessageSnapshot> messages,
            int playerLevel,
            DateTimeOffset cutoff,
            string selfAuthorId,
            string selfAuthorName,
            Action<string> report)
        {
            var offers = new List<DuelOffer>();
            var rejectionCounts = new Dictionary<DuelOfferRejectionReason, int>();
            foreach (var message in messages)
            {
                if (_offerParser.TryParseEligibleCf(
                    message, playerLevel, cutoff, selfAuthorId, selfAuthorName,
                    out var opponentLevel, out var reason))
                {
                    offers.Add(new DuelOffer(channel, message, opponentLevel));
                    continue;
                }

                rejectionCounts[reason] = rejectionCounts.TryGetValue(reason, out var count) ? count + 1 : 1;
            }

            foreach (var rejection in rejectionCounts.OrderBy(item => item.Key))
            {
                report?.Invoke($"#{channel.Name}: skipped {rejection.Value} message(s), reason {rejection.Key}.");
            }

            return offers;
        }
    }
}
