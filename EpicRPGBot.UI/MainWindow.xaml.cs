using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EpicRPGBot.UI.AreaTrading;
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
        private readonly InMemoryLog _log = new InMemoryLog();
        private readonly LastMessagesBuffer _last = new LastMessagesBuffer(5);
        private readonly IDiscordChatClient _botChatClient;
        private readonly IDiscordChatClient _playerChatClient;
        private readonly IDiscordChatClient _guildChatClient;
        private readonly IDiscordChatClient _dungeonChatClient;
        private readonly IDuelDiscordClient _duelChatClient;
        private readonly ConsoleMessageNavigationRouter _consoleMessageNavigationRouter;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly ConfirmedCommandSender _dungeonConfirmedCommandSender;
        private readonly AppSettingsService _settingsService;
        private readonly CooldownTracker _cooldownTracker;
        private readonly CooldownInitializationWorkflow _cooldownWorkflow;
        private readonly PuzzleSelfTestRunner _puzzleSelfTestRunner;
        private readonly DesktopAlertService _alertService;
        private readonly ChatMessagePoller _messagePoller;
        private readonly GuildRaidCoordinator _guildRaidCoordinator;
        private readonly LogCraftingWorkflow _logCraftingWorkflow;
        private readonly DismantlingWorkflow _dismantlingWorkflow;
        private readonly AreaTradeWorkflow _areaTradeWorkflow;
        private readonly CompleteDungeonRunCoordinator _completeDungeonRunCoordinator;
        private readonly DungeonWorkflow _dungeonWorkflow;
        private readonly DuelWorkflow _duelWorkflow;
        private readonly WishingTokenWorkflow _wishingTokenWorkflow;
        private readonly CardDeckImportWorkflow _cardDeckImportWorkflow;
        private readonly AutoBestWorkCommandWorkflow _autoBestWorkCommandWorkflow;
        private readonly HashSet<string> _processedMessageIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _processedMessageOrder = new Queue<string>();

        private BotEngine _engine;
        private bool _isAreaTradeRunning;
        private bool _isDungeonRunning;
        private bool _isDuelRunning;
        private bool _isSleepyPotionRunning;
        private bool _isTimeCookieRunning;
        private bool _isWishingTokenRunning;
        private TimeCookieTarget? _activeTimeCookieTarget;
        private CancellationTokenSource _dungeonCancellation;
        private CancellationTokenSource _duelCancellation;
        private CancellationTokenSource _sleepyPotionCancellation;
        private CancellationTokenSource _timeCookieCancellation;
        private CancellationTokenSource _wishingTokenCancellation;
        private Grid _lastMessagesPanel;

        public MainWindow()
        {
            InitializeComponent();
            ApplyAutomationSurface();

            _botChatClient = new DiscordChatClient(Web, "bot");
            _playerChatClient = new DiscordChatClient(PlayerWeb, "player");
            _guildChatClient = new DiscordChatClient(GuildWeb, "guild");
            _dungeonChatClient = new DiscordChatClient(DungeonWeb, "dungeon");
            _duelChatClient = new DiscordChatClient(
                DuelWeb,
                "duel",
                message => _log.Info("[duel] " + message));
            _consoleMessageNavigationRouter = CreateConsoleMessageNavigationRouter();
            _confirmedCommandSender = new ConfirmedCommandSender(_botChatClient);
            _dungeonConfirmedCommandSender = new ConfirmedCommandSender(_dungeonChatClient);
            _settingsService = new AppSettingsService(new LocalSettingsStore());
            _cooldownTracker = new CooldownTracker(CooldownVisual);
            _cooldownWorkflow = new CooldownInitializationWorkflow(_botChatClient, _cooldownTracker, _settingsService);
            _logCraftingWorkflow = new LogCraftingWorkflow(_confirmedCommandSender);
            _dismantlingWorkflow = new DismantlingWorkflow(_confirmedCommandSender);
            _areaTradeWorkflow = new AreaTradeWorkflow(_confirmedCommandSender, _dismantlingWorkflow, _settingsService, GetCurrentSettings);
            _completeDungeonRunCoordinator = new CompleteDungeonRunCoordinator();
            _dungeonWorkflow = new DungeonWorkflow(_dungeonChatClient, _dungeonConfirmedCommandSender, _settingsService, GetCurrentSettings);
            _duelWorkflow = new DuelWorkflow(
                _duelChatClient,
                _botChatClient,
                _confirmedCommandSender,
                _settingsService);
            _wishingTokenWorkflow = new WishingTokenWorkflow(_botChatClient, _confirmedCommandSender);
            _cardDeckImportWorkflow = new CardDeckImportWorkflow(_botChatClient);
            _autoBestWorkCommandWorkflow = new AutoBestWorkCommandWorkflow(_botChatClient);
            _puzzleSelfTestRunner = new PuzzleSelfTestRunner();
            _alertService = new DesktopAlertService();
            _messagePoller = new ChatMessagePoller(_botChatClient);
            _guildRaidCoordinator = new GuildRaidCoordinator(_guildChatClient, GetCurrentSettings);
            _messagePoller.MessageDetected += OnPolledMessage;

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowWorkAreaChrome.ConstrainToWorkArea(this);
            Env.Load();
            BindUiState();
            _cooldownTracker.Start();

            _log.Engine("UI loaded");
            await RunPuzzleSelfTestIfRequestedAsync();
            await InitializeBrowsersAsync();
            await NavigateStartupTabsAsync();
            HookGuildRaidSettings();
            HookAppSettings();
            await StartGuildRaidWatcherAsync();
            _messagePoller.Start();
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
            _dungeonCancellation?.Cancel();
            _duelCancellation?.Cancel();
            _sleepyPotionCancellation?.Cancel();
            _timeCookieCancellation?.Cancel();
            _wishingTokenCancellation?.Cancel();
            _messagePoller.Stop();
            UnhookGuildRaidSettings();
            UnhookAppSettings();
            _guildRaidCoordinator.Dispose();
            _engine?.Stop();
            _cooldownTracker.Stop();
            ReleaseStatsUi();
            _alertService.Dispose();
        }
    }
}
