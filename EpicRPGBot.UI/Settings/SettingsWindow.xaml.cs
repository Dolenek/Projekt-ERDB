using System;
using System.Threading.Tasks;
using System.Windows;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettingsService _settingsService;
        private readonly Func<Task<CardDeckImportResult>> _loadCardDeck;
        private bool _loadingSettings;

        public SettingsWindow(AppSettingsService settingsService)
            : this(settingsService, null)
        {
        }

        public SettingsWindow(
            AppSettingsService settingsService,
            Func<Task<CardDeckImportResult>> loadCardDeck)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _loadCardDeck = loadCardDeck;
            InitializeComponent();
            ApplyAutomationSurface();
            RegisterSettingsPersistence();
            LoadSettings(_settingsService.LoadCurrent());
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void WorkCommandsBtn_Click(object sender, RoutedEventArgs e)
        {
            var workCommandsWindow = new WorkCommandsWindow(_settingsService)
            {
                Owner = this
            };

            workCommandsWindow.ShowDialog();
        }

        private void GuildRaidBtn_Click(object sender, RoutedEventArgs e)
        {
            var guildRaidWindow = new GuildRaidSettingsWindow(_settingsService)
            {
                Owner = this
            };

            guildRaidWindow.ShowDialog();
        }

        private void CardHandBtn_Click(object sender, RoutedEventArgs e)
        {
            var cardHandWindow = new CardHandSettingsWindow(_settingsService, _loadCardDeck)
            {
                Owner = this
            };

            cardHandWindow.ShowDialog();
        }

    }
}
