using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI
{
    public sealed class BotEngine
    {
        private const int GlobalCommandGapMs = 1000;

        private readonly IDiscordChatClient _chatClient;
        private readonly CaptchaSolverService _captchaSolver;
        private readonly DispatcherTimer _checkMessageTimer;
        private readonly TrackedCommandScheduler _scheduler;
        private readonly SemaphoreSlim _sendGate = new SemaphoreSlim(1, 1);

        private readonly int _area;
        private readonly int _farmCooldown;

        private string _hunt = "rpg hunt h";
        private string _adventure = "rpg adv h";
        private string _work = "rpg chop";
        private string _farm = "rpg farm";
        private string _lastMessageId = string.Empty;
        private string _previousMessageId = string.Empty;
        private string _previousMessageText = string.Empty;
        private bool _running;
        private bool _awaitingStartupCooldownSnapshot;
        private DateTime _lastCommandSentUtc = DateTime.MinValue;

        public BotEngine(WebView2 web, int area, int huntCooldown, int workCooldown, int farmCooldown)
            : this(new DiscordChatClient(web), area, huntCooldown, 61000, workCooldown, farmCooldown)
        {
        }

        public BotEngine(WebView2 web, int area, int huntCooldown, int adventureCooldown, int workCooldown, int farmCooldown)
            : this(new DiscordChatClient(web), area, huntCooldown, adventureCooldown, workCooldown, farmCooldown)
        {
        }

        public BotEngine(IDiscordChatClient chatClient, int area, int huntCooldown, int workCooldown, int farmCooldown)
            : this(chatClient, area, huntCooldown, 61000, workCooldown, farmCooldown)
        {
        }

        public BotEngine(IDiscordChatClient chatClient, int area, int huntCooldown, int adventureCooldown, int workCooldown, int farmCooldown)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _captchaSolver = new CaptchaSolverService(_chatClient);
            _area = area;
            _farmCooldown = farmCooldown;
            _scheduler = new TrackedCommandScheduler(area, huntCooldown, adventureCooldown, workCooldown, farmCooldown, OnTrackedTimerElapsedAsync);
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
        public event Action<string> OnMessageSeen;
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
            _awaitingStartupCooldownSnapshot = false;
            _scheduler.StopAll();
            _scheduler.ClearPending();
            _checkMessageTimer.Stop();
            OnEngineStopped?.Invoke();
        }

        public async Task<bool> SendImmediateAsync(string text)
        {
            var ok = await SendRawWithGlobalCooldownAsync(text);
            if (ok)
            {
                OnCommandSent?.Invoke(text);
            }

            return ok;
        }

        public bool TryInitializeFromCooldownSnapshot(
            TimeSpan? huntRemaining,
            TimeSpan? adventureRemaining,
            TimeSpan? workRemaining,
            TimeSpan? farmRemaining)
        {
            if (!_running || !_awaitingStartupCooldownSnapshot)
            {
                return false;
            }

            _awaitingStartupCooldownSnapshot = false;
            ScheduleFromRemaining(TrackedCommandKind.Hunt, huntRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Adventure, adventureRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Work, workRemaining);
            ScheduleFromRemaining(TrackedCommandKind.Farm, farmRemaining);
            return true;
        }

        private async Task OnTrackedTimerElapsedAsync(TrackedCommandKind kind)
        {
            await SendTrackedCommandAsync(kind, GetCommandText(kind));
        }

        private void ScheduleFromRemaining(TrackedCommandKind kind, TimeSpan? remaining)
        {
            _scheduler.Schedule(kind, remaining.HasValue && remaining.Value > TimeSpan.Zero ? remaining.Value : TimeSpan.Zero, _running);
        }

        private async Task SendTrackedCommandAsync(TrackedCommandKind kind, string command)
        {
            if (!_running)
            {
                return;
            }

            try
            {
                var sent = await SendRawWithGlobalCooldownAsync(command);
                if (!sent)
                {
                    _scheduler.Schedule(kind, TimeSpan.FromSeconds(5), _running);
                    return;
                }

                _scheduler.RegisterPending(kind);
                OnCommandSent?.Invoke(command);
                await SafeDelay(2001);
                await ProcessIncomingMessagesAsync();
            }
            catch
            {
                _scheduler.Schedule(kind, TimeSpan.FromSeconds(5), _running);
            }
        }

        private async Task RespectMinimumCommandGapAsync()
        {
            var elapsed = DateTime.UtcNow - _lastCommandSentUtc;
            if (elapsed < TimeSpan.FromMilliseconds(GlobalCommandGapMs))
            {
                await SafeDelay((int)Math.Ceiling((TimeSpan.FromMilliseconds(GlobalCommandGapMs) - elapsed).TotalMilliseconds));
            }
        }

        private string GetCommandText(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _hunt;
                case TrackedCommandKind.Adventure: return _adventure;
                case TrackedCommandKind.Work: return _work;
                default: return _farm;
            }
        }

        private async Task<bool> SendAndEmitAsync(string text)
        {
            var ok = await SendRawWithGlobalCooldownAsync(text);
            if (ok)
            {
                OnCommandSent?.Invoke(text);
            }

            return ok;
        }

        private async Task<bool> SendRawWithGlobalCooldownAsync(string text)
        {
            await _sendGate.WaitAsync();
            try
            {
                await RespectMinimumCommandGapAsync();
                var ok = await _chatClient.SendMessageAsync(text);
                if (ok)
                {
                    _lastCommandSentUtc = DateTime.UtcNow;
                }

                return ok;
            }
            finally
            {
                _sendGate.Release();
            }
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
                OnMessageSeen?.Invoke(snapshot.Text);
                EventCheck(snapshot.Text ?? string.Empty);
            }
        }

        private async Task<string> CheckLastMessageAsync()
        {
            DiscordMessageSnapshot snapshot;
            try
            {
                snapshot = await _chatClient.GetLatestMessageAsync();
            }
            catch
            {
                return string.Empty;
            }

            if (snapshot == null || string.IsNullOrEmpty(snapshot.Id) || snapshot.Id == _lastMessageId)
            {
                return string.Empty;
            }

            _previousMessageId = _lastMessageId;
            _lastMessageId = snapshot.Id;
            OnMessageSeen?.Invoke(snapshot.Text);
            return snapshot.Text ?? string.Empty;
        }

        private void EventCheck(string message)
        {
            var msg = message ?? string.Empty;
            const string guardAlt = "EPIC GUARD: stop there,";
            const string guardClassic = "Select the item of the image above or respond with the item name";

            if (msg.IndexOf(guardAlt, StringComparison.OrdinalIgnoreCase) >= 0 ||
                msg.IndexOf(guardClassic, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (!string.IsNullOrEmpty(_previousMessageText) &&
                 (_previousMessageText.IndexOf(guardAlt, StringComparison.OrdinalIgnoreCase) >= 0 ||
                  _previousMessageText.IndexOf(guardClassic, StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                var detectionInfo = msg.IndexOf(guardAlt, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    msg.IndexOf(guardClassic, StringComparison.OrdinalIgnoreCase) >= 0
                    ? "Captcha detected in latest message."
                    : "Captcha detected in previous message.";
                OnCaptchaDetected?.Invoke(detectionInfo);
                ReportSolverInfo(detectionInfo);
                _ = SolveCaptchaAsync(detectionInfo.StartsWith("Captcha detected in latest", StringComparison.Ordinal)
                    ? _lastMessageId
                    : _previousMessageId);
            }

            _scheduler.HandleResponse(msg, _running);

            if (msg.IndexOf("TEST", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("I hear you: " + DateTime.Now);
            }
            else if (msg.IndexOf("BOT HELP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("Change work - chop / axe / bowsaw / chainsaw ");
                _ = SendAndEmitAsync("Change farm - farm / potato / carrot / bread");
                _ = SendAndEmitAsync("bot farming - will start farming");
            }
            else if (msg.IndexOf("STOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _scheduler.StopAll();
                _scheduler.ClearPending();
            }
            else if (msg.IndexOf("START", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _awaitingStartupCooldownSnapshot = true;
                _ = SendAndEmitAsync("rpg cd");
                _checkMessageTimer.Start();
            }
            else if (msg.IndexOf("CHANGE WORK", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                HandleChangeWork(msg);
            }
            else if (msg.IndexOf("CHANGE FARM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                HandleChangeFarm(msg);
            }
            else if (msg.IndexOf("BOT FARMING", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("I am farming");
                _scheduler.Schedule(TrackedCommandKind.Farm, TimeSpan.FromMilliseconds(_farmCooldown), _running);
            }
            else if (msg.IndexOf("You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("RUN");
            }
            else if (msg.IndexOf("megarace boost", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("yes");
            }
            else if (msg.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("CUT");
            }
            else if (msg.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("LURE");
            }
            else if (msg.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendRawWithGlobalCooldownAsync("CATCH");
            }
            else if (msg.IndexOf("God accidentally dropped an EPIC coin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg, "I SHALL BRING THE EPIC TO THE COIN", "MY PRECIOUS", "WHAT IS EPIC? THIS COIN", "YES! AN EPIC COIN", "OPERATION: EPIC COIN");
            }
            else if (msg.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg, "BACK OFF THIS IS MINE!!", "HACOINA MATATA", "THIS IS MINE", "ALL THE COINS BELONG TO ME", "GIMME DA MONEY", "OPERATION: COINS");
            }
            else if (msg.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg, "YUP I WILL DO THAT", "I WANT THAT", "HEY EPIC NPC! I WANT TO TRADE WITH YOU", "THAT SOUNDS LIKE AN OP BUSINESS", "OWO ME!!!");
            }
            else if (msg.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("SUMMON");
            }
            else if (msg.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("TIME TO FIGHT");
            }

            _previousMessageText = msg;
        }

        private void HandleChangeWork(string message)
        {
            if (message.IndexOf("CHOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg chop";
                _ = SendAndEmitAsync("I am chopping treez");
            }
            else if (message.IndexOf("AXE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg axe";
                _ = SendAndEmitAsync("I am using an axe");
            }
            else if (message.IndexOf("BOWSAW", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg bowsaw";
                _ = SendAndEmitAsync("I am using a bowsaw");
            }
            else if (message.IndexOf("CHAINSAW", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg chainsaw";
                _ = SendAndEmitAsync("I am using a chainsaw");
            }
        }

        private void HandleChangeFarm(string message)
        {
            if (message.IndexOf("FARM FARM", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm";
                _ = SendAndEmitAsync("I am farming normally");
            }
            else if (message.IndexOf("CARROT", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm carrot";
                _ = SendAndEmitAsync("I am farming carrots");
            }
            else if (message.IndexOf("POTATO", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm potato";
                _ = SendAndEmitAsync("I am farming potatoes");
            }
            else if (message.IndexOf("BREAD", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _farm = "rpg farm bread";
                _ = SendAndEmitAsync("I am farming bread");
            }
        }

        private void RespondFirstPresent(string message, params string[] options)
        {
            if (string.IsNullOrEmpty(message) || options == null || options.Length == 0)
            {
                return;
            }

            foreach (var option in options)
            {
                if (string.IsNullOrEmpty(option) || message.IndexOf(option, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                _ = SendRawWithGlobalCooldownAsync(option);
                return;
            }
        }

        private Task SolveCaptchaAsync(string targetMessageId)
        {
            return _captchaSolver.TrySolveAsync(
                targetMessageId,
                _lastMessageId,
                _previousMessageId,
                SendAndEmitAsync,
                _scheduler.PauseAll,
                () => _scheduler.ResumeAll(_running),
                ReportSolverInfo);
        }

        private void ReportSolverInfo(string info)
        {
            OnSolverInfo?.Invoke(info);
        }

        private static string ResolveWorkCommand(int area)
        {
            if (area >= 3 && area <= 5) return "rpg axe";
            if (area >= 6 && area <= 8) return "rpg bowsaw";
            if (area >= 9 && area <= 13) return "rpg chainsaw";
            return "rpg chop";
        }

        private static Task SafeDelay(int milliseconds)
        {
            return Task.Delay(milliseconds);
        }
    }
}
