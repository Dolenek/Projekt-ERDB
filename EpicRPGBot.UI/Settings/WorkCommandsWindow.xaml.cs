using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Settings
{
    public partial class WorkCommandsWindow : Window
    {
        private readonly AppSettingsService _settingsService;

        public WorkCommandsWindow(AppSettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            Rows = BuildRows(_settingsService.Current);
            DataContext = this;
            InitializeComponent();
            RegisterPersistence();
            ApplyAutomationSurface();
        }

        public ObservableCollection<AreaWorkCommandRow> Rows { get; }

        private static ObservableCollection<AreaWorkCommandRow> BuildRows(AppSettingsSnapshot settings)
        {
            var selections = AreaWorkCommandSettings.Parse(settings?.WorkCommands);
            var rows = new ObservableCollection<AreaWorkCommandRow>();
            for (var area = AreaWorkCommandSettings.MinimumArea; area <= AreaWorkCommandSettings.MaximumArea; area++)
            {
                rows.Add(new AreaWorkCommandRow(area, selections[area]));
            }

            return rows;
        }

        private void RegisterPersistence()
        {
            foreach (var row in Rows)
            {
                row.PropertyChanged += OnRowPropertyChanged;
            }
        }

        private void OnRowPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.Equals(e.PropertyName, nameof(AreaWorkCommandRow.CommandText), StringComparison.Ordinal))
            {
                return;
            }

            PersistSelections();
        }

        private void PersistSelections()
        {
            var selections = new Dictionary<int, string>();
            foreach (var row in Rows)
            {
                selections[row.Area] = row.CommandText;
            }

            var snapshot = _settingsService.Current.WithWorkCommands(AreaWorkCommandSettings.Serialize(selections));
            _settingsService.Save(snapshot);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AreaCommandInput_Loaded(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Tag == null)
            {
                return;
            }

            SetAutomationIdentity(textBox, $"WorkCommandArea{textBox.Tag}Input");
        }

        private void AreaCommandInput_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (!(textBox?.DataContext is AreaWorkCommandRow row))
            {
                return;
            }

            var normalized = AreaWorkCommandSettings.NormalizeCommandText(row.CommandText, row.Area);
            if (string.Equals(row.CommandText, normalized, StringComparison.Ordinal))
            {
                return;
            }

            row.CommandText = normalized;
            textBox.Text = normalized;
        }

        private void ApplyAutomationSurface()
        {
            SetAutomationIdentity(this, "WorkCommandsWindow");
            SetAutomationIdentity(CloseBtn, "WorkCommandsCloseButton");
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
