using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public partial class CardHandSettingsWindow : Window
    {
        private readonly AppSettingsService _settingsService;
        private readonly Func<Task<CardDeckImportResult>> _loadDeck;
        private Dictionary<CardRewardKind, TextBox> _weightBoxes;
        private bool _loading;

        public CardHandSettingsWindow(
            AppSettingsService settingsService,
            Func<Task<CardDeckImportResult>> loadDeck)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _loadDeck = loadDeck;
            InitializeComponent();
            DialogWindowShell.Attach(this, MinimizeBtn, MaximizeRestoreBtn, CloseWindowBtn);
            BuildWeightMap();
            ApplyAutomationSurface();
            LoadSettings(_settingsService.Current.CardHand);
            RegisterPersistence();
        }

        private void BuildWeightMap()
        {
            _weightBoxes = new Dictionary<CardRewardKind, TextBox>
            {
                [CardRewardKind.TimeCapsule] = TimeCapsuleWeightBox,
                [CardRewardKind.RoundCard] = RoundCardWeightBox,
                [CardRewardKind.EternalLootbox] = EternalLootboxWeightBox,
                [CardRewardKind.GodlyLootbox] = GodlyLootboxWeightBox,
                [CardRewardKind.OmegaLootbox] = OmegaLootboxWeightBox,
                [CardRewardKind.Flask] = FlaskWeightBox,
                [CardRewardKind.TimeCookie] = TimeCookieWeightBox,
                [CardRewardKind.GuildRing] = GuildRingWeightBox,
                [CardRewardKind.ArenaCookie] = ArenaCookieWeightBox
            };
        }

        private void RegisterPersistence()
        {
            AutoPlayCheckBox.Checked += OnSettingsChanged;
            AutoPlayCheckBox.Unchecked += OnSettingsChanged;
            foreach (var box in _weightBoxes.Values) box.TextChanged += OnWeightChanged;
        }

        private void LoadSettings(CardHandSettingsSnapshot settings)
        {
            _loading = true;
            AutoPlayCheckBox.IsChecked = settings.AutoPlayEnabled;
            foreach (var pair in _weightBoxes)
                pair.Value.Text = settings.RewardWeights.Get(pair.Key).ToString(CultureInfo.InvariantCulture);
            DeckStatusText.Text = BuildDeckStatus(settings);
            LoadDeckButton.IsEnabled = _loadDeck != null;
            ValidationText.Text = string.Empty;
            _loading = false;
        }

        private static string BuildDeckStatus(CardHandSettingsSnapshot settings)
        {
            if (!settings.IsDeckLoaded) return "No deck loaded. All cards are treated as unowned.";
            var timestamp = settings.DeckLoadedUtc?.ToLocalTime().ToString("g") ?? "unknown time";
            return $"Owned cards: {settings.OwnedCards.Count}/53. Last loaded: {timestamp}.";
        }

        private void OnSettingsChanged(object sender, RoutedEventArgs e) => PersistSettings();
        private void OnWeightChanged(object sender, TextChangedEventArgs e) => PersistSettings();

        private void PersistSettings()
        {
            if (_loading) return;
            if (!TryReadWeights(out var weights))
            {
                ValidationText.Text = "Every reward weight must be a non-negative number.";
                return;
            }

            ValidationText.Text = "Changes saved.";
            var cardHand = _settingsService.Current.CardHand.WithPreferences(
                AutoPlayCheckBox.IsChecked == true,
                weights);
            _settingsService.Save(_settingsService.Current.WithCardHand(cardHand));
        }

        private bool TryReadWeights(out CardHandRewardWeights weights)
        {
            var values = new Dictionary<CardRewardKind, decimal>();
            foreach (var pair in _weightBoxes)
            {
                if (!TryParseWeight(pair.Value.Text, out var value))
                {
                    weights = null;
                    return false;
                }

                values[pair.Key] = value;
            }

            weights = new CardHandRewardWeights(values);
            return true;
        }

        private static bool TryParseWeight(string raw, out decimal value)
        {
            return (decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
                    decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) && value >= 0m;
        }

        private async void LoadDeckButton_Click(object sender, RoutedEventArgs e)
        {
            if (_loadDeck == null) return;
            LoadDeckButton.IsEnabled = false;
            ValidationText.Text = "Loading card deck…";
            var result = await _loadDeck();
            ValidationText.Text = result.Message;
            LoadSettings(_settingsService.Current.CardHand);
            ValidationText.Text = result.Message;
            LoadDeckButton.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void ApplyAutomationSurface()
        {
            SetId(this, "CardHandSettingsWindow");
            SetId(AutoPlayCheckBox, "CardHandAutoPlayInput");
            SetId(LoadDeckButton, "CardHandLoadDeckButton");
            SetId(DeckStatusText, "CardHandDeckStatus");
            SetId(CloseButton, "CardHandSettingsCloseButton");
            SetId(MinimizeBtn, "CardHandSettingsMinimizeButton");
            SetId(MaximizeRestoreBtn, "CardHandSettingsMaximizeRestoreButton");
            SetId(CloseWindowBtn, "CardHandSettingsWindowCloseButton");
            foreach (var pair in _weightBoxes) SetId(pair.Value, "CardHandWeight" + pair.Key);
        }

        private static void SetId(DependencyObject element, string automationId)
        {
            AutomationProperties.SetAutomationId(element, automationId);
        }
    }
}
