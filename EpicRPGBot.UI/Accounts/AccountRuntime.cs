using System;
using System.Collections.Generic;
using EpicRPGBot.UI.AreaTrading;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Dismantling;
using EpicRPGBot.UI.Dungeon;
using EpicRPGBot.UI.Duel;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.WishingToken;
using EpicRPGBot.UI.WorkCommands;

namespace EpicRPGBot.UI.Accounts
{
    public sealed partial class AccountRuntime : IDisposable
    {
        public AccountRuntime(AccountDefinition definition, string settingsPath, AccountBrowserHosts hosts)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (hosts == null) throw new ArgumentNullException(nameof(hosts));

            SettingsService = new AppSettingsService(new LocalSettingsStore(settingsPath, true));
            CreateBrowserSessions(hosts);
            ConfirmedCommandSender = new ConfirmedCommandSender(BotChatClient);
            DungeonConfirmedCommandSender = new ConfirmedCommandSender(DungeonChatClient);
            CooldownTracker = new CooldownTracker(null);
            CooldownWorkflow = new CooldownInitializationWorkflow(
                BotChatClient, CooldownTracker, SettingsService);
            LogCraftingWorkflow = new LogCraftingWorkflow(ConfirmedCommandSender);
            DismantlingWorkflow = new DismantlingWorkflow(ConfirmedCommandSender);
            AreaTradeWorkflow = new AreaTradeWorkflow(
                ConfirmedCommandSender, DismantlingWorkflow, SettingsService, () => SettingsService.Current);
            CompleteDungeonRunCoordinator = new CompleteDungeonRunCoordinator();
            DungeonWorkflow = new DungeonWorkflow(
                DungeonChatClient, DungeonConfirmedCommandSender, SettingsService, () => SettingsService.Current);
            DuelWorkflow = new DuelWorkflow(
                DuelChatClient, BotChatClient, ConfirmedCommandSender, SettingsService);
            WishingTokenWorkflow = new WishingTokenWorkflow(BotChatClient, ConfirmedCommandSender);
            CardDeckImportWorkflow = new CardDeckImportWorkflow(BotChatClient);
            AutoBestWorkCommandWorkflow = new AutoBestWorkCommandWorkflow(BotChatClient);
            MessagePoller = new ChatMessagePoller(BotChatClient);
            GuildRaidCoordinator = new GuildRaidCoordinator(GuildChatClient, () => SettingsService.Current);
        }

        public AccountDefinition Definition { get; }
        public InMemoryLog Log { get; } = new InMemoryLog();
        public LastMessagesBuffer LastMessages { get; } = new LastMessagesBuffer(5);
        public HashSet<string> ProcessedMessageRevisions { get; } =
            new HashSet<string>(StringComparer.Ordinal);
        public Queue<string> ProcessedMessageRevisionOrder { get; } = new Queue<string>();

        public IDiscordChatClient BotChatClient { get; private set; }
        public IDiscordChatClient PlayerChatClient { get; private set; }
        public IDiscordChatClient GuildChatClient { get; private set; }
        public IDiscordChatClient DungeonChatClient { get; private set; }
        public IDuelDiscordClient DuelChatClient { get; private set; }
        public DiscordWebViewSession BotWebViewSession { get; private set; }
        public DiscordWebViewSession PlayerWebViewSession { get; private set; }
        public DiscordWebViewSession GuildWebViewSession { get; private set; }
        public DiscordWebViewSession DungeonWebViewSession { get; private set; }
        public DiscordWebViewSession DuelWebViewSession { get; private set; }
        public ConsoleMessageNavigationRouter ConsoleMessageNavigationRouter { get; set; }
        public ConfirmedCommandSender ConfirmedCommandSender { get; }
        public ConfirmedCommandSender DungeonConfirmedCommandSender { get; }
        public AppSettingsService SettingsService { get; }
        public CooldownTracker CooldownTracker { get; }
        public CooldownInitializationWorkflow CooldownWorkflow { get; }
        public ChatMessagePoller MessagePoller { get; }
        public GuildRaidCoordinator GuildRaidCoordinator { get; }
        public LogCraftingWorkflow LogCraftingWorkflow { get; }
        public DismantlingWorkflow DismantlingWorkflow { get; }
        public AreaTradeWorkflow AreaTradeWorkflow { get; }
        public CompleteDungeonRunCoordinator CompleteDungeonRunCoordinator { get; }
        public DungeonWorkflow DungeonWorkflow { get; }
        public DuelWorkflow DuelWorkflow { get; }
        public WishingTokenWorkflow WishingTokenWorkflow { get; }
        public CardDeckImportWorkflow CardDeckImportWorkflow { get; }
        public AutoBestWorkCommandWorkflow AutoBestWorkCommandWorkflow { get; }

        public event Action StateChanged;

        public void NotifyStateChanged()
        {
            StateChanged?.Invoke();
        }

        private void CreateBrowserSessions(AccountBrowserHosts hosts)
        {
            var accountId = Definition.AccountId.ToString("D");
            var profileName = Definition.BrowserProfileName;
            BotWebViewSession = CreateSession(hosts.Bot, hosts, "bot", ResolveChannelUrl, accountId, profileName, out var bot);
            PlayerWebViewSession = CreateSession(hosts.Player, hosts, "player", ResolveChannelUrl, accountId, profileName, out var player);
            GuildWebViewSession = CreateSession(hosts.Guild, hosts, "guild", ResolveGuildUrl, accountId, profileName, out var guild);
            DungeonWebViewSession = CreateSession(hosts.Dungeon, hosts, "dungeon", ResolveDungeonUrl, accountId, profileName, out var dungeon);
            DuelWebViewSession = CreateSession(hosts.Duel, hosts, "duel", () => DuelChannelCatalog.OutgoingDuelChannelUrl,
                accountId, profileName, out var duel, message => Log.Info("[duel] " + message));
            BotChatClient = bot;
            PlayerChatClient = player;
            GuildChatClient = guild;
            DungeonChatClient = dungeon;
            DuelChatClient = duel;
        }

        private DiscordWebViewSession CreateSession(System.Windows.Controls.ContentControl host,
            AccountBrowserHosts hosts, string role, Func<string> initialUrl, string accountId,
            string profileName, out DiscordChatClient client, Action<string> telemetry = null)
        {
            return DiscordWebViewSession.Create(host, hosts.BackgroundParking, role, initialUrl,
                telemetry, accountId, profileName, out client);
        }

        private string ResolveChannelUrl() => SettingsService.Current.ResolveChannelUrl();
        private string ResolveDungeonUrl() => SettingsService.Current.ResolveDungeonListingChannelUrl();

        private string ResolveGuildUrl()
        {
            return SettingsService.Current.TryResolveGuildRaidChannelUrl(out var url)
                ? url
                : ResolveChannelUrl();
        }
    }
}
