using System;
using System.Collections.Generic;
using System.Threading;
using EpicRPGBot.UI.Accounts;
using EpicRPGBot.UI.AreaTrading;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Dismantling;
using EpicRPGBot.UI.Dungeon;
using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.TimeCookie;
using EpicRPGBot.UI.WishingToken;
using EpicRPGBot.UI.WorkCommands;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private readonly AsyncLocal<AccountRuntime> _operationAccount = new AsyncLocal<AccountRuntime>();
        private readonly SemaphoreSlim _accountSwitchGate = new SemaphoreSlim(1, 1);
        private readonly List<AccountRuntime> _accountRuntimes = new List<AccountRuntime>();
        private AccountRegistry _accountRegistry;
        private IAccountRuntimeFactory _accountRuntimeFactory;
        private AccountRuntime _activeAccountRuntime;

        private AccountRuntime CurrentAccount => _operationAccount.Value ?? _activeAccountRuntime;
        private InMemoryLog _log => CurrentAccount.Log;
        private LastMessagesBuffer _last => CurrentAccount.LastMessages;
        private IDiscordChatClient _botChatClient => CurrentAccount.BotChatClient;
        private IDiscordChatClient _playerChatClient => CurrentAccount.PlayerChatClient;
        private IDiscordChatClient _guildChatClient => CurrentAccount.GuildChatClient;
        private IDiscordChatClient _dungeonChatClient => CurrentAccount.DungeonChatClient;
        private IDuelDiscordClient _duelChatClient => CurrentAccount.DuelChatClient;
        private DiscordWebViewSession _botWebViewSession => CurrentAccount.BotWebViewSession;
        private DiscordWebViewSession _playerWebViewSession => CurrentAccount.PlayerWebViewSession;
        private DiscordWebViewSession _guildWebViewSession => CurrentAccount.GuildWebViewSession;
        private DiscordWebViewSession _dungeonWebViewSession => CurrentAccount.DungeonWebViewSession;
        private DiscordWebViewSession _duelWebViewSession => CurrentAccount.DuelWebViewSession;
        private ConsoleMessageNavigationRouter _consoleMessageNavigationRouter => CurrentAccount.ConsoleMessageNavigationRouter;
        private ConfirmedCommandSender _confirmedCommandSender => CurrentAccount.ConfirmedCommandSender;
        private AppSettingsService _settingsService => CurrentAccount.SettingsService;
        private CooldownTracker _cooldownTracker => CurrentAccount.CooldownTracker;
        private CooldownInitializationWorkflow _cooldownWorkflow => CurrentAccount.CooldownWorkflow;
        private ChatMessagePoller _messagePoller => CurrentAccount.MessagePoller;
        private GuildRaidCoordinator _guildRaidCoordinator => CurrentAccount.GuildRaidCoordinator;
        private LogCraftingWorkflow _logCraftingWorkflow => CurrentAccount.LogCraftingWorkflow;
        private DismantlingWorkflow _dismantlingWorkflow => CurrentAccount.DismantlingWorkflow;
        private AreaTradeWorkflow _areaTradeWorkflow => CurrentAccount.AreaTradeWorkflow;
        private CompleteDungeonRunCoordinator _completeDungeonRunCoordinator => CurrentAccount.CompleteDungeonRunCoordinator;
        private DungeonWorkflow _dungeonWorkflow => CurrentAccount.DungeonWorkflow;
        private DuelWorkflow _duelWorkflow => CurrentAccount.DuelWorkflow;
        private WishingTokenWorkflow _wishingTokenWorkflow => CurrentAccount.WishingTokenWorkflow;
        private CardDeckImportWorkflow _cardDeckImportWorkflow => CurrentAccount.CardDeckImportWorkflow;
        private AutoBestWorkCommandWorkflow _autoBestWorkCommandWorkflow => CurrentAccount.AutoBestWorkCommandWorkflow;
        private HashSet<string> _processedMessageRevisions => CurrentAccount.ProcessedMessageRevisions;
        private Queue<string> _processedMessageRevisionOrder => CurrentAccount.ProcessedMessageRevisionOrder;

        private BotEngine _engine { get => CurrentAccount.Engine; set => CurrentAccount.Engine = value; }
        private bool _isAreaTradeRunning { get => CurrentAccount.IsAreaTradeRunning; set => CurrentAccount.IsAreaTradeRunning = value; }
        private bool _isDungeonRunning { get => CurrentAccount.IsDungeonRunning; set => CurrentAccount.IsDungeonRunning = value; }
        private bool _isDuelRunning { get => CurrentAccount.IsDuelRunning; set => CurrentAccount.IsDuelRunning = value; }
        private bool _duelInitialEngineWasRunning { get => CurrentAccount.DuelInitialEngineWasRunning; set => CurrentAccount.DuelInitialEngineWasRunning = value; }
        private bool _isSleepyPotionRunning { get => CurrentAccount.IsSleepyPotionRunning; set => CurrentAccount.IsSleepyPotionRunning = value; }
        private bool _isTimeCookieRunning { get => CurrentAccount.IsTimeCookieRunning; set => CurrentAccount.IsTimeCookieRunning = value; }
        private bool _isWishingTokenRunning { get => CurrentAccount.IsWishingTokenRunning; set => CurrentAccount.IsWishingTokenRunning = value; }
        private TimeCookieTarget? _activeTimeCookieTarget { get => CurrentAccount.ActiveTimeCookieTarget; set => CurrentAccount.ActiveTimeCookieTarget = value; }
        private CancellationTokenSource _dungeonCancellation { get => CurrentAccount.DungeonCancellation; set => CurrentAccount.DungeonCancellation = value; }
        private CancellationTokenSource _duelCancellation { get => CurrentAccount.DuelCancellation; set => CurrentAccount.DuelCancellation = value; }
        private CancellationTokenSource _sleepyPotionCancellation { get => CurrentAccount.SleepyPotionCancellation; set => CurrentAccount.SleepyPotionCancellation = value; }
        private CancellationTokenSource _timeCookieCancellation { get => CurrentAccount.TimeCookieCancellation; set => CurrentAccount.TimeCookieCancellation = value; }
        private CancellationTokenSource _wishingTokenCancellation { get => CurrentAccount.WishingTokenCancellation; set => CurrentAccount.WishingTokenCancellation = value; }

        private IDisposable UseAccount(AccountRuntime runtime)
        {
            return new AccountOperationScope(_operationAccount, runtime);
        }

        private void RunForAccount(AccountRuntime runtime, Action action)
        {
            using (UseAccount(runtime)) action();
        }

        private sealed class AccountOperationScope : IDisposable
        {
            private readonly AsyncLocal<AccountRuntime> _accountSlot;
            private readonly AccountRuntime _previous;

            public AccountOperationScope(AsyncLocal<AccountRuntime> accountSlot, AccountRuntime account)
            {
                _accountSlot = accountSlot;
                _previous = accountSlot.Value;
                accountSlot.Value = account;
            }

            public void Dispose()
            {
                _accountSlot.Value = _previous;
            }
        }
    }
}
