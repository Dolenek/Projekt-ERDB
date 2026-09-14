using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EpicRPGBot.UI.Accounts;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void InitializeAccountRuntimes(
            AccountRegistry registry,
            IAccountRuntimeFactory runtimeFactory)
        {
            _accountRegistry = registry ?? throw new ArgumentNullException(nameof(registry));
            _accountRuntimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
            var snapshot = _accountRegistry.Load();
            foreach (var definition in snapshot.Accounts)
            {
                _accountRuntimes.Add(CreateAccountRuntime(definition));
            }

            _activeAccountRuntime = _accountRuntimes.First(item =>
                item.Definition.AccountId == snapshot.SelectedAccountId);
            _activeAccountRuntime.CooldownTracker.BindVisual(CooldownVisual);
            RefreshAccountStrip();
        }

        private AccountRuntime CreateAccountRuntime(AccountDefinition definition)
        {
            var hosts = new AccountBrowserHosts(
                Web, PlayerWeb, GuildWeb, DungeonWeb, DuelWeb, BackgroundBrowserParking);
            var settingsPath = _accountRegistry.ResolveSettingsPath(definition);
            var runtime = _accountRuntimeFactory.Create(definition, settingsPath, hosts);
            runtime.ConsoleMessageNavigationRouter = CreateConsoleMessageNavigationRouter(runtime);
            HookRuntimeCallbacks(runtime);
            return runtime;
        }

        private void HookRuntimeCallbacks(AccountRuntime runtime)
        {
            runtime.PollerMessageHandler = snapshot =>
                RunForAccount(runtime, () => HandleObservedMessage(snapshot));
            runtime.MessagePoller.MessageDetected += runtime.PollerMessageHandler;
            runtime.CooldownStatsHandler = snapshot =>
            {
                if (ReferenceEquals(runtime, _activeAccountRuntime))
                    RunForAccount(runtime, () => OnCooldownStatsChanged(snapshot));
            };
            runtime.CooldownTracker.StatsChanged += runtime.CooldownStatsHandler;
            runtime.StateChanged += RefreshAccountStrip;
        }

        private async Task StartAccountRuntimesAsync()
        {
            foreach (var runtime in _accountRuntimes)
            {
                using (UseAccount(runtime))
                {
                    runtime.CooldownTracker.Start();
                    HookGuildRaidSettings();
                    HookAppSettings();
                    await StartGuildRaidWatcherAsync();
                }
            }

            using (UseAccount(_activeAccountRuntime))
            {
                await InitializeBrowsersAsync();
            }
        }

        private void DisposeAccountRuntimes()
        {
            foreach (var runtime in _accountRuntimes)
            {
                using (UseAccount(runtime))
                {
                    UnhookGuildRaidSettings();
                    UnhookAppSettings();
                    runtime.MessagePoller.MessageDetected -= runtime.PollerMessageHandler;
                    runtime.CooldownTracker.StatsChanged -= runtime.CooldownStatsHandler;
                }
                runtime.Dispose();
            }
        }

        private async void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as FrameworkElement)?.Tag is AccountRuntime runtime)
            {
                await ActivateAccountAsync(runtime);
            }
        }

        private async void AddAccountBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AccountNameDialog("Add account") { Owner = this };
            if (dialog.ShowDialog() != true) return;
            var definition = _accountRegistry.Add(dialog.AccountName);
            var runtime = CreateAccountRuntime(definition);
            _accountRuntimes.Add(runtime);
            runtime.CooldownTracker.Start();
            using (UseAccount(runtime))
            {
                HookGuildRaidSettings();
                HookAppSettings();
                await StartGuildRaidWatcherAsync();
            }
            RefreshAccountStrip();
            await ActivateAccountAsync(runtime);
        }

        private async void RenameAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as FrameworkElement)?.Tag is AccountRuntime runtime)) return;
            var dialog = new AccountNameDialog("Rename account", runtime.Definition.DisplayName) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            _accountRegistry.Rename(runtime.Definition.AccountId, dialog.AccountName);
            runtime.Definition.Rename(dialog.AccountName);
            RefreshAccountStrip();
            await Task.CompletedTask;
        }

        private async Task ActivateAccountAsync(AccountRuntime nextRuntime)
        {
            if (nextRuntime == null || ReferenceEquals(nextRuntime, _activeAccountRuntime)) return;
            await _accountSwitchGate.WaitAsync();
            try
            {
                if (ReferenceEquals(nextRuntime, _activeAccountRuntime)) return;
                var previousRuntime = _activeAccountRuntime;
                SaveAccountUiState(previousRuntime);
                previousRuntime.CooldownTracker.UnbindVisual();
                await ReleaseSelectedAccountBrowserAsync(previousRuntime);

                _activeAccountRuntime = nextRuntime;
                _accountRegistry.Select(nextRuntime.Definition.AccountId);
                nextRuntime.CooldownTracker.BindVisual(CooldownVisual);
                RestoreAccountUiState(nextRuntime);
                RefreshAccountStrip();

                using (UseAccount(nextRuntime))
                {
                    if (!nextRuntime.BrowserSessionsInitialized) await InitializeBrowsersAsync();
                    else await ReconcileSelectedBrowserSessionAsync();
                }
            }
            finally
            {
                _accountSwitchGate.Release();
            }
        }

        private async Task ReleaseSelectedAccountBrowserAsync(AccountRuntime runtime)
        {
            if (runtime.SelectedBrowserSession == null) return;
            using (UseAccount(runtime))
            {
                await SetSelectedDemandSafeAsync(runtime.SelectedBrowserSession, false);
                runtime.SelectedBrowserSession = null;
                ReconcileMessagePolling();
            }
        }

        private void SaveAccountUiState(AccountRuntime runtime)
        {
            runtime.ActivitySearchText = ActivitySearchBox.Text ?? string.Empty;
            runtime.SelectedLogKind = GetSelectedLogKind();
            runtime.SelectedActivityPanel = StatsPanel.Visibility == Visibility.Visible
                ? 1
                : ConsolePanel.Visibility == Visibility.Visible ? 2 : 0;
            runtime.SelectedBrowserRole = GetBrowserRole(BrowserTabs.SelectedItem as TabItem);
        }

        private void RestoreAccountUiState(AccountRuntime runtime)
        {
            ActivitySearchBox.Text = runtime.ActivitySearchText;
            SelectLogKind(runtime.SelectedLogKind);
            BindActivityUi();
            ShowAccountActivityPanel(runtime.SelectedActivityPanel);
            ApplyAccountPanelWidths(runtime);
            BrowserTabs.SelectedItem = GetBrowserTab(runtime.SelectedBrowserRole);
            UpdateSentCountTexts();
            ApplyCooldownStats(runtime.CooldownTracker.GetStatsSnapshot());
            RefreshBotControlButtonColors();
        }
    }
}
