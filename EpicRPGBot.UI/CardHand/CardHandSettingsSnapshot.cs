using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicRPGBot.UI.CardHand
{
    public sealed class CardHandSettingsSnapshot
    {
        public CardHandSettingsSnapshot(
            bool autoPlayEnabled,
            CardHandRewardWeights rewardWeights,
            bool isDeckLoaded,
            IEnumerable<CardId> ownedCards,
            DateTime? deckLoadedUtc)
        {
            AutoPlayEnabled = autoPlayEnabled;
            RewardWeights = rewardWeights ?? throw new ArgumentNullException(nameof(rewardWeights));
            IsDeckLoaded = isDeckLoaded;
            OwnedCards = new HashSet<CardId>(ownedCards ?? Enumerable.Empty<CardId>());
            DeckLoadedUtc = deckLoadedUtc;
        }

        public bool AutoPlayEnabled { get; }
        public CardHandRewardWeights RewardWeights { get; }
        public bool IsDeckLoaded { get; }
        public IReadOnlyCollection<CardId> OwnedCards { get; }
        public DateTime? DeckLoadedUtc { get; }

        public static CardHandSettingsSnapshot Default => new CardHandSettingsSnapshot(
            true,
            CardHandRewardWeights.Default,
            false,
            Enumerable.Empty<CardId>(),
            null);

        public CardHandSettingsSnapshot WithPreferences(bool enabled, CardHandRewardWeights weights)
        {
            return new CardHandSettingsSnapshot(enabled, weights, IsDeckLoaded, OwnedCards, DeckLoadedUtc);
        }

        public CardHandSettingsSnapshot WithDeck(IEnumerable<CardId> ownedCards, DateTime loadedUtc)
        {
            return new CardHandSettingsSnapshot(AutoPlayEnabled, RewardWeights, true, ownedCards, loadedUtc);
        }
    }
}
