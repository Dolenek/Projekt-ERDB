using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Duel
{
    internal sealed class DuelOfferClaimManager
    {
        private readonly IDuelDiscordClient _chatClient;
        private readonly DuelOfferScanner _offerScanner;

        public DuelOfferClaimManager(IDuelDiscordClient chatClient, DuelOfferScanner offerScanner)
        {
            _chatClient = chatClient;
            _offerScanner = offerScanner;
        }

        public async Task<DuelOffer> ClaimAsync(
            DuelOffer offer,
            DuelProfileContext profile,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var current = await _offerScanner.RevalidateAsync(
                offer,
                profile.Level,
                profile.DiscordAuthorId,
                profile.DiscordAuthorName,
                cancellationToken);
            if (current == null)
            {
                report?.Invoke($"Skipped stale or claimed offer {offer.Message.Id}.");
                return null;
            }

            if (!await _chatClient.AddReactionAsync(current.Message.Id, "white_check_mark", cancellationToken))
            {
                report?.Invoke($"Skipped offer {current.Message.Id}: ✅ could not be added.");
                return null;
            }

            return current;
        }

        public async Task MarkAfkAsync(
            DuelOffer offer,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (!await NavigateToOfferAsync(offer, "add :AFK:", report, cancellationToken))
            {
                return;
            }

            await _chatClient.GetMessagesSinceAsync(
                GetOfferLoadCutoff(offer),
                cancellationToken);
            var added = await _chatClient.AddReactionAsync(offer.Message.Id, "AFK", cancellationToken);
            report?.Invoke(added ? "Marked the unanswered offer as :AFK:." : "Could not add :AFK: reaction.");
        }

        public async Task ReleaseAsync(
            DuelOffer offer,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (!await NavigateToOfferAsync(offer, "release the ✅ claim", report, cancellationToken))
            {
                return;
            }

            await _chatClient.GetMessagesSinceAsync(
                GetOfferLoadCutoff(offer),
                cancellationToken);
            var removed = await _chatClient.RemoveOwnReactionAsync(
                offer.Message.Id,
                "white_check_mark",
                cancellationToken);
            report?.Invoke(removed ? "Released the unused ✅ claim." : "Could not release the unused ✅ claim.");
        }

        private async Task<bool> NavigateToOfferAsync(
            DuelOffer offer,
            string purpose,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (await _chatClient.NavigateToChannelAndWaitAsync(offer.Channel.Url, cancellationToken))
            {
                return true;
            }

            report?.Invoke($"Could not return to the original offer to {purpose}.");
            return false;
        }

        private static DateTimeOffset GetOfferLoadCutoff(DuelOffer offer)
        {
            return offer.Message.CreatedAtUtc?.AddSeconds(-1) ??
                   DateTimeOffset.UtcNow - TimeSpan.FromMinutes(15);
        }
    }
}
