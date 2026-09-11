using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.CardHand
{
    public static class CardHandSettingsPersistence
    {
        private const string Prefix = "card_hand_";

        public static CardHandSettingsSnapshot Read(LocalSettingsStore store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            var defaults = CardHandSettingsSnapshot.Default;
            var weights = new Dictionary<CardRewardKind, decimal>();
            foreach (CardRewardKind kind in Enum.GetValues(typeof(CardRewardKind)))
            {
                var raw = store.GetString(Prefix + "weight_" + ToKey(kind), string.Empty);
                weights[kind] = ParseWeight(raw, defaults.RewardWeights.Get(kind));
            }

            var deckLoaded = store.GetBool(Prefix + "deck_loaded", false);
            var owned = ParseCards(store.GetString(Prefix + "owned_cards", string.Empty));
            var loadedUtc = ParseDate(store.GetString(Prefix + "deck_loaded_utc", string.Empty));
            return new CardHandSettingsSnapshot(
                store.GetBool(Prefix + "enabled", defaults.AutoPlayEnabled),
                new CardHandRewardWeights(weights),
                deckLoaded,
                owned,
                loadedUtc);
        }

        public static void Write(LocalSettingsStore store, CardHandSettingsSnapshot settings)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            store.SetBool(Prefix + "enabled", settings.AutoPlayEnabled);
            foreach (CardRewardKind kind in Enum.GetValues(typeof(CardRewardKind)))
            {
                store.SetString(
                    Prefix + "weight_" + ToKey(kind),
                    settings.RewardWeights.Get(kind).ToString(CultureInfo.InvariantCulture));
            }

            store.SetBool(Prefix + "deck_loaded", settings.IsDeckLoaded);
            store.SetString(Prefix + "owned_cards", string.Join(",", settings.OwnedCards.OrderBy(card => card)));
            store.SetString(Prefix + "deck_loaded_utc", settings.DeckLoadedUtc?.ToString("O") ?? string.Empty);
        }

        private static IEnumerable<CardId> ParseCards(string serialized)
        {
            var cards = new HashSet<CardId>();
            foreach (var raw in (serialized ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (CardId.TryParse(raw, out var card)) cards.Add(card);
            }

            return cards;
        }

        private static decimal ParseWeight(string raw, decimal fallback)
        {
            return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value >= 0m
                ? value
                : fallback;
        }

        private static DateTime? ParseDate(string raw)
        {
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value)
                ? value.ToUniversalTime()
                : (DateTime?)null;
        }

        private static string ToKey(CardRewardKind kind)
        {
            return System.Text.RegularExpressions.Regex.Replace(kind.ToString(), "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
        }
    }
}
