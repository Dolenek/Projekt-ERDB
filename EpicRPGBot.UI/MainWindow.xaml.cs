using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EpicRPGBot.UI.AreaTrading;
using EpicRPGBot.UI.Accounts;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Dismantling;
using EpicRPGBot.UI.Dungeon;
using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.TimeCookie;
using EpicRPGBot.UI.WishingToken;
using EpicRPGBot.UI.WorkCommands;

namespace EpicRPGBot.UI
{
    public partial class MainWindow : Window
    {
        private readonly PuzzleSelfTestRunner _puzzleSelfTestRunner;
        private readonly DesktopAlertService _alertService;
        private Grid _lastMessagesPanel;

        public MainWindow() : this(new AccountRegistry(), new AccountRuntimeFactory())
        {
        }

        internal MainWindow(AccountRegistry accountRegistry, IAccountRuntimeFactory accountRuntimeFactory)
        {
            InitializeComponent();
            ApplyAutomationSurface();
            _puzzleSelfTestRunner = new PuzzleSelfTestRunner();
            _alertService = new DesktopAlertService();
            InitializeAccountRuntimes(accountRegistry, accountRuntimeFactory);

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowWorkAreaChrome.ConstrainToWorkArea(this);
            Env.Load();
            BindUiState();

            _log.Engine("UI loaded");
            await RunPuzzleSelfTestIfRequestedAsync();
            await StartAccountRuntimesAsync();
        }

        private void BindUiState()
        {
            _lastMessagesPanel = (Grid)FindName("LastMessagesPanel");
            BindActivityUi();
            ShowSidebarPanel(lastMessagesVisible: true, statsVisible: false, consoleVisible: false);
            BindStatsUi();
            RefreshBotControlButtonColors();
        }

        private async Task RunPuzzleSelfTestIfRequestedAsync()
        {
            try
            {
                if (string.Equals(Env.Get("PUZZLE_SELFTEST", null), "1", StringComparison.OrdinalIgnoreCase))
                {
                    await _puzzleSelfTestRunner.RunAsync(_log.Info);
                }
            }
            catch
            {
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            ReleaseStatsUi();
            _alertService.Dispose();
            DisposeAccountRuntimes();
        }
    }
}
