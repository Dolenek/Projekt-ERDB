using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow : Window
    {
        private readonly InMemoryLog _log = new InMemoryLog();
        private readonly LastMessagesBuffer _last = new LastMessagesBuffer(5);
        private readonly IDiscordChatClient _botChatClient;
        private readonly IDiscordChatClient _playerChatClient;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly AppSettingsService _settingsService;
        private readonly CooldownTracker _cooldownTracker;
        private readonly CooldownInitializationWorkflow _cooldownWorkflow;
        private readonly CaptchaSelfTestRunner _captchaSelfTestRunner;
        private readonly DesktopAlertService _alertService;
        private readonly ChatMessagePoller _messagePoller;
        private readonly LogCraftingWorkflow _logCraftingWorkflow;
        private readonly HashSet<string> _processedMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedMessageOrder = new Queue<string>();

        private BotEngine _engine;
        private Grid _lastMessagesPanel;

        public MainWindow()
        {
            InitializeComponent();
            ApplyAutomationSurface();

            _botChatClient = new DiscordChatClient(Web, "bot");
            _playerChatClient = new DiscordChatClient(PlayerWeb, "player");
            _confirmedCommandSender = new ConfirmedCommandSender(_botChatClient);
            _settingsService = new AppSettingsService(new LocalSettingsStore());
            _cooldownTracker = new CooldownTracker(this);
            _cooldownWorkflow = new CooldownInitializationWorkflow(_botChatClient, _cooldownTracker, _settingsService);
            _logCraftingWorkflow = new LogCraftingWorkflow(_confirmedCommandSender);
            _captchaSelfTestRunner = new CaptchaSelfTestRunner();
            _alertService = new DesktopAlertService();
            _messagePoller = new ChatMessagePoller(_botChatClient);
            _messagePoller.MessageDetected += OnPolledMessage;

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Env.Load();
            BindUiState();
            _cooldownTracker.Start();

            _log.Engine("UI loaded");
            await RunCaptchaSelfTestIfRequestedAsync();
            await InitializeBrowsersAsync();
            await NavigateStartupTabsAsync();
            _messagePoller.Start();
        }

        private void BindUiState()
        {
            StatsList.ItemsSource = _last.Items;
            ConsoleList.ItemsSource = _log.Items;
            _lastMessagesPanel = (Grid)FindName("LastMessagesPanel");
            BindStatsUi();
        }

        private async Task RunCaptchaSelfTestIfRequestedAsync()
        {
            try
            {
                if (string.Equals(Env.Get("CAPTCHA_SELFTEST", null), "1", StringComparison.OrdinalIgnoreCase))
                {
                    await _captchaSelfTestRunner.RunAsync(_log.Info);
                }
            }
            catch
            {
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _messagePoller.Stop();
            _engine?.Stop();
            _cooldownTracker.Stop();
            ReleaseStatsUi();
            _alertService.Dispose();
        }
    }
}
