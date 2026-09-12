using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EpicRPGBot.UI.Pets
{
    public partial class PetsWindow : Window
    {
        private readonly IPetGateway _gateway;
        private readonly Action<string> _report;
        private readonly PetSettingsStore _settings;
        private readonly PetFusionPlanner _planner = new PetFusionPlanner();
        private readonly ObservableCollection<PetRow> _rows = new ObservableCollection<PetRow>();
        private PetInventory _inventory;
        private CancellationTokenSource _cancellation;
        private bool _ready;
        private bool _blocked;
        public bool SafeToResume { get; private set; } = true;
        public bool UserStopped { get; private set; }

        public PetsWindow(IPetGateway gateway, Action<string> report, PetSettingsStore settings = null)
        {
            _gateway = gateway;
            _report = report;
            _settings = settings ?? new PetSettingsStore();
            InitializeComponent();
            LoadOptions(_settings.Load());
            PetGrid.ItemsSource = _rows;
            CollectionViewSource.GetDefaultView(_rows).Filter = FilterPet;
            _ready = true;
            Loaded += async (sender, args) => await RefreshAsync(false);
            Closing += OnClosing;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs args) => await RefreshAsync(false);
        private async void Reset_Click(object sender, RoutedEventArgs args) => await RefreshAsync(true);
        private void Close_Click(object sender, RoutedEventArgs args) => Close();
        private void Stop_Click(object sender, RoutedEventArgs args)
        {
            UserStopped = true;
            _cancellation?.Cancel();
            Report("Stop requested; waiting for the current operation to settle.");
        }

        private async Task RefreshAsync(bool reset)
        {
            if (_cancellation != null) return;
            SetBusy(true);
            try
            {
                var request = ReadRequest();
                var refreshed = await _gateway.LoadAsync(_cancellation.Token);
                if (reset) request = new PetFusionRequest { Options = ReadOptions() };
                else if (_inventory != null) PetIdentityMapper.Remap(_inventory, refreshed, request);
                else request.LockedIds = _settings.LoadLocks(refreshed);
                _blocked = false;
                DisplayInventory(refreshed, request);
                SaveSelections();
                Report(reset ? "Selections and locks reset. Review protections before selecting pets." : "All pet pages loaded.");
            }
            catch (Exception exception) { Fail(exception); }
            finally { SetBusy(false); }
        }

        private async void Start_Click(object sender, RoutedEventArgs args)
        {
            if (_inventory == null || _blocked || _cancellation != null) return;
            var request = ReadRequest();
            if (!_planner.Next(_inventory, request).Available) return;
            if (PetManualFusionWarning.Required(request) && MessageBox.Show(this,
                PetManualFusionWarning.Describe(request), "Confirm manual group fusion",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            SaveSelections();
            SetBusy(true);
            try
            {
                var workflow = new PetFusionWorkflow(_gateway, _planner);
                await workflow.RunAsync(_inventory, request, updated => DisplayInventory(updated, request), Report, _cancellation.Token);
                SaveSelections();
            }
            catch (Exception exception) { Fail(exception); }
            finally { SetBusy(false); }
        }

        private void DisplayInventory(PetInventory inventory, PetFusionRequest request)
        {
            _ready = false;
            _inventory = inventory;
            foreach (var row in _rows) row.PropertyChanged -= RowChanged;
            _rows.Clear();
            foreach (var pet in inventory.Pets)
            {
                var row = new PetRow { Pet = pet, Selected = request.MaterialIds.Contains(pet.Id), Locked = request.LockedIds.Contains(pet.Id) };
                row.PropertyChanged += RowChanged;
                _rows.Add(row);
            }
            TargetBox.ItemsSource = inventory.Pets.Select(p => p.Id).ToArray();
            TargetBox.SelectedItem = request.TargetId;
            CountText.Text = inventory.Owner + " · " + inventory.Pets.Count + " pets";
            _ready = true;
            RefreshPreview();
        }

        private void SetBusy(bool busy)
        {
            if (busy) _cancellation = new CancellationTokenSource();
            else { _cancellation?.Dispose(); _cancellation = null; }
            RefreshButton.IsEnabled = ResetButton.IsEnabled = SelectButton.IsEnabled = !busy;
            PetGrid.IsEnabled = ProtectionPanel.IsEnabled = PlanPanel.IsEnabled = !busy;
            CloseButton.IsEnabled = !busy;
            StopButton.IsEnabled = busy;
            RefreshPreview();
        }

        private void Fail(Exception exception)
        {
            if (exception is OperationCanceledException) { Report("Operation cancelled."); return; }
            SafeToResume = false;
            _blocked = true;
            Report(exception.Message + " Bot automation will remain stopped.");
        }

        private void Report(string message)
        {
            LogBox.AppendText(message + Environment.NewLine);
            LogBox.ScrollToEnd();
            _report(message);
        }

        private void OnClosing(object sender, CancelEventArgs args)
        {
            if (_cancellation != null) { args.Cancel = true; Stop_Click(sender, new RoutedEventArgs()); return; }
            _settings.Save(ReadOptions());
            if (!_blocked) SaveSelections();
        }
    }
}
