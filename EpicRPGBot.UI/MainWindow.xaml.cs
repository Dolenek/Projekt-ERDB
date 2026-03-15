using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow : Window
    {
        private readonly InMemoryLog _log = new InMemoryLog();
        private readonly LastMessagesBuffer _last = new LastMessagesBuffer(5);
        private readonly IDiscordChatClient _chatClient;
        private readonly LocalSettingsStore _settingsStore;
        private readonly CooldownTracker _cooldownTracker;
        private readonly CooldownInitializationWorkflow _cooldownWorkflow;
        private readonly CaptchaSelfTestRunner _captchaSelfTestRunner;
        private readonly DesktopAlertService _alertService;
        private readonly ChatMessagePoller _messagePoller;

        private BotEngine _engine;
        private Grid _lastMessagesPanel;
        private TextBlock _huntCountText;
        private int _huntCount;
        private bool _loadingSettings;

        public MainWindow()
        {
            InitializeComponent();
            ApplyAutomationSurface();

            _chatClient = new DiscordChatClient(Web);
            _settingsStore = new LocalSettingsStore();
            _cooldownTracker = new CooldownTracker(this);
            _cooldownWorkflow = new CooldownInitializationWorkflow(_chatClient, _cooldownTracker, _settingsStore);
            _captchaSelfTestRunner = new CaptchaSelfTestRunner();
            _alertService = new DesktopAlertService();
            _messagePoller = new ChatMessagePoller(_chatClient);
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
            await InitializeBrowserAsync();
            await NavigateToCurrentChannelAsync();
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

        private async Task InitializeBrowserAsync()
        {
            try
            {
                SetInitHint("Initializing WebView2...");
                await _chatClient.EnsureInitializedAsync();
                InitHint.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                SetInitHint("WebView2 init failed: " + ex.Message);
            }
        }

        private async Task NavigateToCurrentChannelAsync()
        {
            try
            {
                await _chatClient.NavigateToChannelAsync(GetChannelUrl());
            }
            catch (Exception ex)
            {
                SetInitHint("Navigate failed: " + ex.Message);
            }
        }

        private string GetChannelUrl()
        {
            var url = ChannelUrlBox.Text?.Trim();
            if (string.IsNullOrEmpty(url) && UseAtMeFallback.IsChecked == true)
            {
                return "https://discord.com/channels/@me";
            }

            return string.IsNullOrEmpty(url) ? "https://discord.com/channels/@me" : url;
        }

        private void OnPolledMessage(string message)
        {
            UiDispatcher.OnUI(() =>
            {
                _last.Add(message);
                _cooldownTracker.ApplyMessage(message);
                TryInitializeEngineFromCooldownSnapshot(message);
            });
        }

        private void ReloadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _chatClient.Reload();
            }
            catch
            {
            }
        }

        private async void GoChannelBtn_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToCurrentChannelAsync();
        }

        private async void InitBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_chatClient.IsReady)
            {
                _log.Info("WebView2 not ready");
                return;
            }

            await _cooldownWorkflow.RunAsync(
                _log.Info,
                ms => HuntCdBox.Text = ms.ToString(),
                ms => AdventureCdBox.Text = ms.ToString(),
                ms => WorkCdBox.Text = ms.ToString(),
                ms => FarmCdBox.Text = ms.ToString(),
                SafeInt(AdventureCdBox.Text, 61000),
                SafeInt(WorkCdBox.Text, 99000),
                SafeInt(FarmCdBox.Text, 196000));
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            _log.Info("Start button clicked");

            if (_engine != null && _engine.IsRunning)
            {
                _log.Info("Engine already running, Start ignored.");
                return;
            }

            _engine = new BotEngine(
                _chatClient,
                SafeInt(AreaBox.Text, 10),
                SafeInt(HuntCdBox.Text, 21000),
                SafeInt(AdventureCdBox.Text, 61000),
                SafeInt(WorkCdBox.Text, 99000),
                SafeInt(FarmCdBox.Text, 196000));

            WireEngineEvents(_engine);

            var sent = await _engine.SendImmediateAsync("rpg cd");
            _log.Info(sent ? "Sent 'rpg cd' immediately." : "Failed to send 'rpg cd'.");

            _engine.Start();
            _log.Engine("Engine started (waiting for cooldown snapshot before scheduling commands)");
        }

        private void WireEngineEvents(BotEngine engine)
        {
            engine.OnCommandSent += command =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Command($"Message ({command}) sent");
                    ApplySentCommandCooldown(command);
                    if (!string.IsNullOrWhiteSpace(command) &&
                        command.Trim().StartsWith("rpg hunt", StringComparison.OrdinalIgnoreCase))
                    {
                        _huntCount++;
                        if (_huntCountText != null)
                        {
                            _huntCountText.Text = $"Hunt sent: {_huntCount}";
                        }
                    }
                });
            };

            engine.OnCaptchaDetected += info =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Warning("[guard] " + info);
                    _alertService.ShowCaptchaAlert(this);
                });
            };

            engine.OnMessageSeen += message =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _cooldownTracker.ApplyMessage(message);
                    TryInitializeEngineFromCooldownSnapshot(message);
                });
            };

            engine.OnSolverInfo += info =>
            {
                UiDispatcher.OnUI(() => _log.Info("[solver] " + info));
            };
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _alertService.Dispose();
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            _engine?.Stop();
            _log.Engine("Engine stopped");
        }

        private void LastMessagesTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMessagesPanel != null)
            {
                _lastMessagesPanel.Visibility = Visibility.Visible;
            }

            StatsPanel.Visibility = Visibility.Collapsed;
            ConsolePanel.Visibility = Visibility.Collapsed;
        }

        private void StatsTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMessagesPanel != null)
            {
                _lastMessagesPanel.Visibility = Visibility.Collapsed;
            }

            StatsPanel.Visibility = Visibility.Visible;
            ConsolePanel.Visibility = Visibility.Collapsed;
        }

        private void ConsoleTabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_lastMessagesPanel != null)
            {
                _lastMessagesPanel.Visibility = Visibility.Collapsed;
            }

            StatsPanel.Visibility = Visibility.Collapsed;
            ConsolePanel.Visibility = Visibility.Visible;
        }

        private void SetInitHint(string text)
        {
            InitHint.Visibility = Visibility.Visible;
            InitHint.Text = text;
        }

        private static int SafeInt(string value, int defaultValue)
        {
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
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

            _settingsStore.SetString("channel_url", ChannelUrlBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetBool("use_at_me_fallback", UseAtMeFallback.IsChecked == true);
            _settingsStore.SetString("area", AreaBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("hunt_ms", HuntCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("adventure_ms", AdventureCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("work_ms", WorkCdBox.Text?.Trim() ?? string.Empty);
            _settingsStore.SetString("farm_ms", FarmCdBox.Text?.Trim() ?? string.Empty);
        }

        private void ApplySentCommandCooldown(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                return;
            }

            var normalized = command.Trim().ToLowerInvariant();
            if (normalized.StartsWith("rpg hunt", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("hunt", SafeInt(HuntCdBox.Text, 61000));
            }
            else if (normalized.StartsWith("rpg adv", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("adventure", SafeInt(AdventureCdBox.Text, 61000));
            }
            else if (normalized.StartsWith("rpg farm", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("farm", SafeInt(FarmCdBox.Text, 196000));
            }
            else if (normalized.StartsWith("rpg chop", StringComparison.Ordinal) ||
                     normalized.StartsWith("rpg axe", StringComparison.Ordinal) ||
                     normalized.StartsWith("rpg bowsaw", StringComparison.Ordinal) ||
                     normalized.StartsWith("rpg chainsaw", StringComparison.Ordinal))
            {
                _cooldownTracker.SetCooldown("work", SafeInt(WorkCdBox.Text, 99000));
            }
        }

        private void TryInitializeEngineFromCooldownSnapshot(string message)
        {
            if (_engine == null || !_engine.IsRunning || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.IndexOf("cooldowns", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var initialized = _engine.TryInitializeFromCooldownSnapshot(
                _cooldownTracker.GetRemaining("hunt"),
                _cooldownTracker.GetRemaining("adventure"),
                _cooldownTracker.GetRemaining("work"),
                _cooldownTracker.GetRemaining("farm"));

            if (initialized)
            {
                _log.Engine("Cooldown snapshot received; command scheduling initialized");
            }
        }
    }
}
