using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public partial class GuildRaidSettingsWindow : Window
    {
        private readonly AppSettingsService _settingsService;
        private bool _loadingSettings;

        public GuildRaidSettingsWindow(AppSettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            InitializeComponent();
            ApplyAutomationSurface();
            RegisterSettingsPersistence();
            LoadSettings(_settingsService.LoadCurrent());
        }

        private void RegisterSettingsPersistence()
        {
            GuildRaidChannelUrlBox.TextChanged += OnSettingsChanged;
            GuildRaidTriggerTextBox.TextChanged += OnSettingsChanged;
            GuildRaidAuthorFilterBox.TextChanged += OnSettingsChanged;
            GuildRaidMatchModeBox.SelectionChanged += OnMatchModeChanged;
        }

        private void LoadSettings(AppSettingsSnapshot settings)
        {
            _loadingSettings = true;

            GuildRaidChannelUrlBox.Text = settings.GuildRaidChannelUrl;
            GuildRaidTriggerTextBox.Text = settings.GuildRaidTriggerText;
            GuildRaidAuthorFilterBox.Text = settings.GuildRaidAuthorFilter;
            SelectMatchMode(settings.GuildRaidMatchMode);

            _loadingSettings = false;
        }

        private void OnSettingsChanged(object sender, TextChangedEventArgs e)
        {
            PersistSettings();
        }

        private void OnMatchModeChanged(object sender, SelectionChangedEventArgs e)
        {
            PersistSettings();
        }

        private void PersistSettings()
        {
            if (_loadingSettings)
            {
                return;
            }

            var updated = _settingsService.Current
                .WithGuildRaidChannelUrl(GuildRaidChannelUrlBox.Text?.Trim() ?? string.Empty)
                .WithGuildRaidTriggerText(GuildRaidTriggerTextBox.Text?.Trim() ?? string.Empty)
                .WithGuildRaidMatchMode(GetSelectedMatchMode())
                .WithGuildRaidAuthorFilter(GuildRaidAuthorFilterBox.Text?.Trim() ?? string.Empty);
            _settingsService.Save(updated);
        }

        private string GetSelectedMatchMode()
        {
            var selectedItem = GuildRaidMatchModeBox.SelectedItem as ComboBoxItem;
            var tag = selectedItem?.Tag as string;
            return string.Equals(tag, GuildRaidMatchModes.Exact, StringComparison.OrdinalIgnoreCase)
                ? GuildRaidMatchModes.Exact
                : GuildRaidMatchModes.Contains;
        }

        private void SelectMatchMode(string matchMode)
        {
            var desiredMode = string.Equals(matchMode, GuildRaidMatchModes.Exact, StringComparison.OrdinalIgnoreCase)
                ? GuildRaidMatchModes.Exact
                : GuildRaidMatchModes.Contains;

            foreach (var item in GuildRaidMatchModeBox.Items)
            {
                var comboBoxItem = item as ComboBoxItem;
                if (comboBoxItem == null)
                {
                    continue;
                }

                var tag = comboBoxItem.Tag as string;
                if (!string.Equals(tag, desiredMode, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GuildRaidMatchModeBox.SelectedItem = comboBoxItem;
                return;
            }

            GuildRaidMatchModeBox.SelectedIndex = 0;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "GuildRaidSettingsWindow");
            SetAutomationIdentity(GuildRaidChannelUrlBox, "GuildRaidSettingsChannelUrlInput");
            SetAutomationIdentity(GuildRaidTriggerTextBox, "GuildRaidSettingsTriggerInput");
            SetAutomationIdentity(GuildRaidMatchModeBox, "GuildRaidSettingsMatchModeInput");
            SetAutomationIdentity(GuildRaidAuthorFilterBox, "GuildRaidSettingsAuthorFilterInput");
            SetAutomationIdentity(CloseBtn, "GuildRaidSettingsCloseButton");
        }

        private static void SetAutomationIdentity(DependencyObject element, string automationId)
        {
            if (element == null || string.IsNullOrWhiteSpace(automationId))
            {
                return;
            }

            AutomationProperties.SetAutomationId(element, automationId);
        }
    }
}
