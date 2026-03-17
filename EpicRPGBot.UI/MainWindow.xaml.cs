using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
        private readonly LocalSettingsStore _settingsStore;
        private readonly CooldownTracker _cooldownTracker;
        private readonly CooldownInitializationWorkflow _cooldownWorkflow;
        private readonly CaptchaSelfTestRunner _captchaSelfTestRunner;
        private readonly DesktopAlertService _alertService;
        private readonly ChatMessagePoller _messagePoller;
        private readonly HashSet<string> _processedMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedMessageOrder = new Queue<string>();

        private BotEngine _engine;
        private Grid _lastMessagesPanel;
        private TextBlock _huntCountText;
        private int _huntCount;
        private bool _loadingSettings;

        public MainWindow()
        {
            InitializeComponent();
            ApplyAutomationSurface();

            _botChatClient = new DiscordChatClient(Web, "bot");
            _playerChatClient = new DiscordChatClient(PlayerWeb, "player");
            _confirmedCommandSender = new ConfirmedCommandSender(_botChatClient);
            _settingsStore = new LocalSettingsStore();
            _cooldownTracker = new CooldownTracker(this);
            _cooldownWorkflow = new CooldownInitializationWorkflow(_botChatClient, _cooldownTracker, _settingsStore);
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
            RegisterSettingsPersistence();
            LoadStoredSettings();
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
            _huntCountText = (TextBlock)FindName("HuntCountText");
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

        private void LoadStoredSettings()
        {
            _loadingSettings = true;

            ChannelUrlBox.Text = _settingsStore.GetString("channel_url", "https://discord.com/channels/@me");
            UseAtMeFallback.IsChecked = _settingsStore.GetBool("use_at_me_fallback", true);
            AreaBox.Text = _settingsStore.GetString("area", AreaBox.Text);
            HuntCdBox.Text = _settingsStore.GetString("hunt_ms", "61000");
            AdventureCdBox.Text = _settingsStore.GetString("adventure_ms", "61000");
            WorkCdBox.Text = _settingsStore.GetString("work_ms", "99000");
            FarmCdBox.Text = _settingsStore.GetString("farm_ms", "196000");
            LootboxCdBox.Text = _settingsStore.GetString("lootbox_ms", "21600000");

            _loadingSettings = false;
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
            _alertService.Dispose();
        }

        private static int SafeInt(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }
    }
}
