using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettingsService _settingsService;
        private bool _loadingSettings;

        public SettingsWindow(AppSettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            InitializeComponent();
            ApplyAutomationSurface();
            RegisterSettingsPersistence();
            LoadSettings(_settingsService.LoadCurrent());
        }

        private void RegisterSettingsPersistence()
        {
            ChannelUrlBox.TextChanged += OnSettingsChanged;
            DungeonListingChannelUrlBox.TextChanged += OnSettingsChanged;
            AreaBox.TextChanged += OnSettingsChanged;
            HuntCdBox.TextChanged += OnSettingsChanged;
            AdventureCdBox.TextChanged += OnSettingsChanged;
            TrainingCdBox.TextChanged += OnSettingsChanged;
            WorkCdBox.TextChanged += OnSettingsChanged;
            FarmCdBox.TextChanged += OnSettingsChanged;
            LootboxCdBox.TextChanged += OnSettingsChanged;
            AscendedCheckBox.Checked += OnAscendedChanged;
            AscendedCheckBox.Unchecked += OnAscendedChanged;
            AutoDeleteDungeonChannelCheckBox.Checked += OnAutoDeleteDungeonChannelChanged;
            AutoDeleteDungeonChannelCheckBox.Unchecked += OnAutoDeleteDungeonChannelChanged;
            UseAtMeFallback.Checked += OnFallbackChanged;
            UseAtMeFallback.Unchecked += OnFallbackChanged;
        }

        private void LoadSettings(AppSettingsSnapshot settings)
        {
            _loadingSettings = true;

            ChannelUrlBox.Text = settings.ChannelUrl;
            DungeonListingChannelUrlBox.Text = settings.DungeonListingChannelUrl;
            UseAtMeFallback.IsChecked = settings.UseAtMeFallback;
            AreaBox.Text = settings.Area;
            AscendedCheckBox.IsChecked = settings.Ascended;
            HuntCdBox.Text = settings.HuntMs;
            AdventureCdBox.Text = settings.AdventureMs;
            TrainingCdBox.Text = settings.TrainingMs;
            WorkCdBox.Text = settings.WorkMs;
            FarmCdBox.Text = settings.FarmMs;
            LootboxCdBox.Text = settings.LootboxMs;
            AutoDeleteDungeonChannelCheckBox.IsChecked = settings.AutoDeleteDungeonChannel;

            _loadingSettings = false;
        }

        private void OnSettingsChanged(object sender, TextChangedEventArgs e)
        {
            PersistSettings();
        }

        private void OnFallbackChanged(object sender, RoutedEventArgs e)
        {
            PersistSettings();
        }

        private void OnAscendedChanged(object sender, RoutedEventArgs e)
        {
            PersistSettings();
        }

        private void OnAutoDeleteDungeonChannelChanged(object sender, RoutedEventArgs e)
        {
            PersistSettings();
        }

        private void PersistSettings()
        {
            if (_loadingSettings)
            {
                return;
            }

            _settingsService.Save(new AppSettingsSnapshot(
                ChannelUrlBox.Text?.Trim() ?? string.Empty,
                DungeonListingChannelUrlBox.Text?.Trim() ?? string.Empty,
                UseAtMeFallback.IsChecked == true,
                AreaBox.Text?.Trim() ?? string.Empty,
                AscendedCheckBox.IsChecked == true,
                HuntCdBox.Text?.Trim() ?? string.Empty,
                AdventureCdBox.Text?.Trim() ?? string.Empty,
                TrainingCdBox.Text?.Trim() ?? string.Empty,
                WorkCdBox.Text?.Trim() ?? string.Empty,
                FarmCdBox.Text?.Trim() ?? string.Empty,
                LootboxCdBox.Text?.Trim() ?? string.Empty,
                _settingsService.Current.WorkCommands,
                _settingsService.Current.ProfilePlayerName,
                AutoDeleteDungeonChannelCheckBox.IsChecked == true,
                _settingsService.Current.GuildRaidChannelUrl,
                _settingsService.Current.GuildRaidTriggerText,
                _settingsService.Current.GuildRaidMatchMode,
                _settingsService.Current.GuildRaidAuthorFilter));
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

        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "SettingsWindow");
            SetAutomationIdentity(ChannelUrlBox, "SettingsChannelUrlInput");
            SetAutomationIdentity(DungeonListingChannelUrlBox, "SettingsDungeonListingChannelUrlInput");
            SetAutomationIdentity(UseAtMeFallback, "SettingsUseAtMeFallback");
            SetAutomationIdentity(AreaBox, "SettingsAreaInput");
            SetAutomationIdentity(AscendedCheckBox, "SettingsAscendedInput");
            SetAutomationIdentity(AutoDeleteDungeonChannelCheckBox, "SettingsAutoDeleteDungeonChannelInput");
            SetAutomationIdentity(HuntCdBox, "SettingsHuntCooldownInput");
            SetAutomationIdentity(AdventureCdBox, "SettingsAdventureCooldownInput");
            SetAutomationIdentity(TrainingCdBox, "SettingsTrainingCooldownInput");
            SetAutomationIdentity(WorkCdBox, "SettingsWorkCooldownInput");
            SetAutomationIdentity(FarmCdBox, "SettingsFarmCooldownInput");
            SetAutomationIdentity(LootboxCdBox, "SettingsLootboxCooldownInput");
            SetAutomationIdentity(WorkCommandsBtn, "SettingsWorkCommandsButton");
            SetAutomationIdentity(GuildRaidBtn, "SettingsGuildRaidButton");
            SetAutomationIdentity(CloseBtn, "SettingsCloseButton");
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
