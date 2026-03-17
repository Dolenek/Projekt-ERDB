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
            AreaBox.TextChanged += OnSettingsChanged;
            HuntCdBox.TextChanged += OnSettingsChanged;
            AdventureCdBox.TextChanged += OnSettingsChanged;
            WorkCdBox.TextChanged += OnSettingsChanged;
            FarmCdBox.TextChanged += OnSettingsChanged;
            LootboxCdBox.TextChanged += OnSettingsChanged;
            UseAtMeFallback.Checked += OnFallbackChanged;
            UseAtMeFallback.Unchecked += OnFallbackChanged;
        }

        private void LoadSettings(AppSettingsSnapshot settings)
        {
            _loadingSettings = true;

            ChannelUrlBox.Text = settings.ChannelUrl;
            UseAtMeFallback.IsChecked = settings.UseAtMeFallback;
            AreaBox.Text = settings.Area;
            HuntCdBox.Text = settings.HuntMs;
            AdventureCdBox.Text = settings.AdventureMs;
            WorkCdBox.Text = settings.WorkMs;
            FarmCdBox.Text = settings.FarmMs;
            LootboxCdBox.Text = settings.LootboxMs;

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

        private void PersistSettings()
        {
            if (_loadingSettings)
            {
                return;
            }

            _settingsService.Save(new AppSettingsSnapshot(
                ChannelUrlBox.Text?.Trim() ?? string.Empty,
                UseAtMeFallback.IsChecked == true,
                AreaBox.Text?.Trim() ?? string.Empty,
                HuntCdBox.Text?.Trim() ?? string.Empty,
                AdventureCdBox.Text?.Trim() ?? string.Empty,
                WorkCdBox.Text?.Trim() ?? string.Empty,
                FarmCdBox.Text?.Trim() ?? string.Empty,
                LootboxCdBox.Text?.Trim() ?? string.Empty));
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "SettingsWindow");
            SetAutomationIdentity(ChannelUrlBox, "SettingsChannelUrlInput");
            SetAutomationIdentity(UseAtMeFallback, "SettingsUseAtMeFallback");
            SetAutomationIdentity(AreaBox, "SettingsAreaInput");
            SetAutomationIdentity(HuntCdBox, "SettingsHuntCooldownInput");
            SetAutomationIdentity(AdventureCdBox, "SettingsAdventureCooldownInput");
            SetAutomationIdentity(WorkCdBox, "SettingsWorkCooldownInput");
            SetAutomationIdentity(FarmCdBox, "SettingsFarmCooldownInput");
            SetAutomationIdentity(LootboxCdBox, "SettingsLootboxCooldownInput");
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
