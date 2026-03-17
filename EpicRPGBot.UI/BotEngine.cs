using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private const int GlobalCommandGapMs = 1000;

        private readonly IDiscordChatClient _chatClient;
        private readonly CaptchaSolverService _captchaSolver;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly DispatcherTimer _checkMessageTimer;
        private readonly TrackedCommandScheduler _scheduler;
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);

        private readonly int _area;
        private readonly int _farmCooldown;

        private string _hunt = "rpg hunt h";
        private string _adventure = "rpg adv h";
        private string _work = "rpg chop";
        private string _farm = "rpg farm";
        private string _lootbox = "rpg buy ed lb";
        private string _lastMessageId = string.Empty;
        private string _previousMessageId = string.Empty;
        private string _previousMessageText = string.Empty;
        private int _queuedCooldownSnapshot;
        private bool _running;
        private bool _awaitingStartupCooldownSnapshot;
        private DateTime _lastCommandSentUtc = DateTime.MinValue;

        public BotEngine(WebView2 web, int area, int huntCooldown, int workCooldown, int farmCooldown, int lootboxCooldown)
            : this(new DiscordChatClient(web), area, huntCooldown, 61000, workCooldown, farmCooldown, lootboxCooldown)
        {
        }

        public BotEngine(WebView2 web, int area, int huntCooldown, int adventureCooldown, int workCooldown, int farmCooldown, int lootboxCooldown)
            : this(new DiscordChatClient(web), area, huntCooldown, adventureCooldown, workCooldown, farmCooldown, lootboxCooldown)
        {
        }

        public BotEngine(IDiscordChatClient chatClient, int area, int huntCooldown, int workCooldown, int farmCooldown, int lootboxCooldown)
            : this(chatClient, area, huntCooldown, 61000, workCooldown, farmCooldown, lootboxCooldown)
        {
        }

        public BotEngine(IDiscordChatClient chatClient, int area, int huntCooldown, int adventureCooldown, int workCooldown, int farmCooldown, int lootboxCooldown)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _captchaSolver = new CaptchaSolverService(_chatClient);
            _confirmedCommandSender = new ConfirmedCommandSender(_chatClient);
            _area = area;
            _farmCooldown = farmCooldown;
            _scheduler = new TrackedCommandScheduler(area, huntCooldown, adventureCooldown, workCooldown, farmCooldown, lootboxCooldown, OnTrackedTimerElapsedAsync);
            _checkMessageTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };

            _checkMessageTimer.Tick += async (sender, args) => await CheckLastMessageForEventsAsync();
        }

        public bool IsRunning => _running;

        public event Action OnEngineStarted;
        public event Action OnEngineStopped;
        public event Action<string> OnCommandSent;
        public event Action<DiscordMessageSnapshot> OnMessageSeen;
        public event Action<string> OnCaptchaDetected;
        public event Action<string> OnSolverInfo;

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _running = true;
            _work = ResolveWorkCommand(_area);
            _awaitingStartupCooldownSnapshot = true;
            _checkMessageTimer.Start();
            OnEngineStarted?.Invoke();
        }

        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            _queuedCooldownSnapshot = 0;
            _awaitingStartupCooldownSnapshot = false;
            _scheduler.StopAll();
            _scheduler.ClearPending();
            _checkMessageTimer.Stop();
            OnEngineStopped?.Invoke();
        }

        public bool QueueCooldownSnapshotRequest()
        {
            if (!_running || Interlocked.Exchange(ref _queuedCooldownSnapshot, 1) == 1)
            {
                return false;
            }

            _ = SendQueuedCooldownSnapshotAsync();
            return true;
        }

        public bool TryInitializeFromCooldownSnapshot(
            TimeSpan? huntRemaining,
            TimeSpan? adventureRemaining,
            TimeSpan? workRemaining,
            TimeSpan? farmRemaining,
            TimeSpan? lootboxRemaining)
        {
            if (!_running || !_awaitingStartupCooldownSnapshot)
            {
                return false;
            }

            _awaitingStartupCooldownSnapshot = false;
            ApplyTrackedCooldownSnapshot(huntRemaining, adventureRemaining, workRemaining, farmRemaining, lootboxRemaining);
            return true;
        }

        public void SyncTrackedCooldowns(
            TimeSpan? huntRemaining,
            TimeSpan? adventureRemaining,
            TimeSpan? workRemaining,
            TimeSpan? farmRemaining,
            TimeSpan? lootboxRemaining)
        {
            if (!_running)
            {
                return;
            }

            ApplyTrackedCooldownSnapshot(huntRemaining, adventureRemaining, workRemaining, farmRemaining, lootboxRemaining);
        }

        private void ApplyTrackedCooldownSnapshot(
            TimeSpan? huntRemaining,
            TimeSpan? adventureRemaining,
            TimeSpan? workRemaining,
            TimeSpan? farmRemaining,
            TimeSpan? lootboxRemaining)
        {
            _scheduler.ClearPending();
            ScheduleFromRemaining(TrackedCommandKind.Hunt, huntRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Adventure, adventureRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Work, workRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Farm, farmRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Lootbox, lootboxRemaining);
        }

        private void ScheduleFromRemaining(TrackedCommandKind kind, TimeSpan? remaining)
        {
            _scheduler.Schedule(kind, remaining.HasValue && remaining.Value > TimeSpan.Zero ? remaining.Value : TimeSpan.Zero, _running);
        }

        private async Task CheckLastMessageForEventsAsync()
        {
            try
            {
                await ProcessIncomingMessagesAsync();
            }
            catch
            {
            }
        }

        private async Task ProcessIncomingMessagesAsync()
        {
            var snapshots = await _chatClient.GetRecentMessagesAsync(8);
            if (snapshots == null || snapshots.Count == 0)
            {
                return;
            }

            var startIndex = 0;
            if (!string.IsNullOrEmpty(_lastMessageId))
            {
                for (var i = 0; i < snapshots.Count; i++)
                {
                    if (string.Equals(snapshots[i].Id, _lastMessageId, StringComparison.Ordinal))
                    {
                        startIndex = i + 1;
                        break;
                    }
                }
            }

            for (var i = startIndex; i < snapshots.Count; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Id) || string.Equals(snapshot.Id, _lastMessageId, StringComparison.Ordinal))
                {
                    continue;
                }

                _previousMessageId = _lastMessageId;
                _lastMessageId = snapshot.Id;
                OnMessageSeen?.Invoke(snapshot);
                _scheduler.HandleResponse(snapshot, _running);
                EventCheck(snapshot.Text ?? string.Empty);
            }
        }
    }
}
