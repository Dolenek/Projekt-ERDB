using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.CardHand;

public sealed class CardHandSettingsPersistenceTests
{
    [Fact]
    public void WriteAndRead_RoundTripsPreferencesAndDeck()
    {
        var fileName = "card-hand-test-" + Guid.NewGuid().ToString("N") + ".ini";
        var store = new LocalSettingsStore(fileName);
        var loadedUtc = new DateTime(2026, 9, 10, 12, 30, 0, DateTimeKind.Utc);
        var values = new Dictionary<CardRewardKind, decimal>(CardHandRewardWeights.Default.ToDictionary());
        values[CardRewardKind.RoundCard] = 73.5m;
        var expected = new CardHandSettingsSnapshot(
            false,
            new CardHandRewardWeights(values),
            true,
            CardHandTestCards.Cards("HA", "D10", "JOKER"),
            loadedUtc);

        try
        {
            CardHandSettingsPersistence.Write(store, expected);
            var actual = CardHandSettingsPersistence.Read(store);

            Assert.False(actual.AutoPlayEnabled);
            Assert.True(actual.IsDeckLoaded);
            Assert.Equal(73.5m, actual.RewardWeights.Get(CardRewardKind.RoundCard));
            Assert.Equal(loadedUtc, actual.DeckLoadedUtc);
            Assert.Equal(expected.OwnedCards.OrderBy(card => card), actual.OwnedCards.OrderBy(card => card));
        }
        finally
        {
            DeleteSettingsFile(fileName);
        }
    }

    [Fact]
    public void Read_InvalidValuesUseSafeDefaults()
    {
        var fileName = "card-hand-test-" + Guid.NewGuid().ToString("N") + ".ini";
        var store = new LocalSettingsStore(fileName);

        try
        {
            store.SetString("card_hand_weight_round_card", "-4");
            store.SetString("card_hand_owned_cards", "HA,invalid,HA");
            store.SetBool("card_hand_deck_loaded", true);
            var actual = CardHandSettingsPersistence.Read(store);

            Assert.Equal(50m, actual.RewardWeights.Get(CardRewardKind.RoundCard));
            Assert.Single(actual.OwnedCards);
            Assert.Contains(CardHandTestCards.Card("HA"), actual.OwnedCards);
        }
        finally
        {
            DeleteSettingsFile(fileName);
        }
    }

    private static void DeleteSettingsFile(string fileName)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EpicRPGBot.UI",
            "settings");
        var path = Path.GetFullPath(Path.Combine(root, fileName));
        if (path.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)) File.Delete(path);
    }
}
