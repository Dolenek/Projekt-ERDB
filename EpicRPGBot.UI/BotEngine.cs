using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;
<<<<<<< Updated upstream
=======
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
>>>>>>> Stashed changes

namespace EpicRPGBot.UI
{
    public sealed class BotEngine
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly CaptchaSolverService _captchaSolver;
        private readonly DispatcherTimer _huntTimer;
        private readonly DispatcherTimer _workTimer;
        private readonly DispatcherTimer _farmTimer;
        private readonly DispatcherTimer _checkMessageTimer;
        private readonly Stopwatch _commandDelay = new Stopwatch();

        private readonly int _area;
        private readonly int _farmCooldown;

        private string _hunt = "rpg hunt h";
        private string _work = "rpg chop";
        private string _farm = "rpg farm";
        private string _lastMessageId = string.Empty;
<<<<<<< Updated upstream

=======
        private string _previousMessageId = string.Empty;
        private string _previousMessageText = string.Empty;
        private int _huntMarryTracker;
>>>>>>> Stashed changes
        private bool _running;

        public BotEngine(WebView2 web, int area, int huntCooldown, int workCooldown, int farmCooldown)
            : this(new DiscordChatClient(web), area, huntCooldown, workCooldown, farmCooldown)
        {
        }

        public BotEngine(IDiscordChatClient chatClient, int area, int huntCooldown, int workCooldown, int farmCooldown)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _captchaSolver = new CaptchaSolverService(_chatClient);
            _area = area;
            _farmCooldown = farmCooldown;

            _huntTimer = CreateTimer(huntCooldown, () => SendCommandAsync(_hunt));
            _workTimer = CreateTimer(workCooldown, () => SendCommandAsync(_work));
            _farmTimer = CreateTimer(farmCooldown, () => SendCommandAsync(_farm));
            _checkMessageTimer = CreateTimer(2000, CheckLastMessageForEventsAsync);
        }

        public bool IsRunning => _running;

        public event Action OnEngineStarted;
        public event Action OnEngineStopped;
        public event Action<string> OnCommandSent;
        public event Action<string> OnMessageSeen;
<<<<<<< Updated upstream

        // Tracks manual "rpg cd" to avoid duplicate opening cd when Start() runs
        private DateTime _lastManualCdUtc = DateTime.MinValue;

        public BotEngine(WebView2 web, int area, int huntCooldown, int workCooldown, int farmCooldown)
        {
            _web = web ?? throw new ArgumentNullException(nameof(web));

            _area = area;
            _huntCooldown = huntCooldown;
            _workCooldown = workCooldown;
            _farmCooldown = farmCooldown;

            _huntT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_huntCooldown) };
            _huntT.Tick += async (s, e) => await SendCommandAsync(_hunt);

            _workT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_workCooldown) };
            _workT.Tick += async (s, e) => await SendCommandAsync(_work);

            _farmT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_farmCooldown) };
            _farmT.Tick += async (s, e) => await SendCommandAsync(_farm);

            _checkMessageT = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2000) };
            _checkMessageT.Tick += async (s, e) =>
            {
                try
                {
                    var msg = await CheckLastMessageAsync();
                    if (!string.IsNullOrEmpty(msg))
                    {
                        EventCheck(msg);
                    }
                }
                catch { }
            };
        }

=======
        public event Action<string> OnSolverInfo;
>>>>>>> Stashed changes

        public void Start()
        {
            if (_running)
            {
                return;
            }

            _running = true;
            _work = ResolveWorkCommand(_area);
            _commandDelay.Restart();
            OnEngineStarted?.Invoke();

            _ = RunStartSequenceAsync(includeCd: false);
            StartTimers();
        }

        public void Stop()
        {
            if (!_running)
            {
                return;
            }

            _running = false;
            StopTimers();
            OnEngineStopped?.Invoke();
        }

        public async Task<bool> SendImmediateAsync(string text)
        {
            var ok = await _chatClient.SendMessageAsync(text);
            if (ok)
            {
                OnCommandSent?.Invoke(text);
            }

            return ok;
        }

        private static DispatcherTimer CreateTimer(int intervalMs, Func<Task> action)
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };

            timer.Tick += async (sender, args) => await action();
            return timer;
        }

        private void StartTimers()
        {
            _huntTimer.Start();
            _workTimer.Start();
            if (_area >= 4)
            {
                _farmTimer.Start();
            }

            _checkMessageTimer.Start();
        }

<<<<<<< Updated upstream
=======
        private void PauseWorkTimers()
        {
            _huntTimer.Stop();
            _workTimer.Stop();
            _farmTimer.Stop();
        }

        private void ResumeWorkTimers()
        {
            _huntTimer.Start();
            _workTimer.Start();
            if (_area >= 4)
            {
                _farmTimer.Start();
            }
        }

>>>>>>> Stashed changes
        private void StopTimers()
        {
            _huntTimer.Stop();
            _workTimer.Stop();
            _farmTimer.Stop();
            _checkMessageTimer.Stop();
        }

        private async Task SendCommandAsync(string command)
        {
            if (!_running)
            {
                return;
            }

            try
            {
                command = NormalizeHuntCommand(command);

                if (_commandDelay.ElapsedMilliseconds <= 2000)
                {
                    await SafeDelay(2000);
                }

                var sent = await _chatClient.SendMessageAsync(command);
                if (!sent)
                {
                    return;
                }

                OnCommandSent?.Invoke(command);
                _commandDelay.Restart();
                await SafeDelay(2001);

                var message = await CheckLastMessageAsync();
                if (!string.IsNullOrEmpty(message))
                {
                    EventCheck(message);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SendCommand error: " + ex.Message);
            }
        }

        private string NormalizeHuntCommand(string command)
        {
            if (!string.Equals(command, "rpg hunt h", StringComparison.Ordinal))
            {
                return command;
            }

            _huntMarryTracker = _huntMarryTracker == 0 ? 1 : 0;
            return "rpg hunt h";
        }

        private async Task<bool> SendAndEmitAsync(string text)
        {
            var ok = await _chatClient.SendMessageAsync(text);
            if (ok)
            {
                OnCommandSent?.Invoke(text);
            }

            return ok;
        }

        private async Task CheckLastMessageForEventsAsync()
        {
            try
            {
                var message = await CheckLastMessageAsync();
                if (!string.IsNullOrEmpty(message))
                {
                    EventCheck(message);
                }
            }
            catch
            {
            }
        }

        private async Task<string> CheckLastMessageAsync()
        {
            DiscordMessageSnapshot snapshot;
            try
            {
<<<<<<< Updated upstream
                var json = await _web.CoreWebView2.ExecuteScriptAsync(js);
                var s = UnquoteJson(json);
                var id = ExtractField(s, "id");
                var text = ExtractField(s, "text");

                if (string.IsNullOrEmpty(id) || id == _lastMessageId) return string.Empty;
                _lastMessageId = id;
                OnMessageSeen?.Invoke(text);
                return text ?? string.Empty;
=======
                snapshot = await _chatClient.GetLatestMessageAsync();
>>>>>>> Stashed changes
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
            var inCurrent = msg.IndexOf(guardAlt, StringComparison.OrdinalIgnoreCase) >= 0;
            var inPrevious = !string.IsNullOrEmpty(_previousMessageText)
                && _previousMessageText.IndexOf(guardAlt, StringComparison.OrdinalIgnoreCase) >= 0;

<<<<<<< Updated upstream
            if (msg.IndexOf("Select the item of the image above or respond with the item name", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Debug.WriteLine("Guard seen");
=======
            if (inCurrent || inPrevious)
            {
                Debug.WriteLine("Guard seen (alt phrase)");
                ReportSolverInfo("Captcha detected (alt phrase).");
                var targetId = inCurrent ? _lastMessageId : _previousMessageId;
                _ = SolveCaptchaAsync(targetId);
>>>>>>> Stashed changes
            }
            else if (msg.IndexOf("TEST", StringComparison.OrdinalIgnoreCase) >= 0)
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
                StopTimers();
            }
            else if (msg.IndexOf("START", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = RunStartSequenceAsync(includeCd: true);
                StartTimers();
            }
            else if (msg.IndexOf("wait at least", StringComparison.OrdinalIgnoreCase) >= 0)
            {
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
                if (!_farmTimer.IsEnabled)
                {
                    _farmTimer.Interval = TimeSpan.FromMilliseconds(_farmCooldown);
                    _farmTimer.Start();
                }
            }
            else if (msg.IndexOf("You were about to hunt a defenseless monster, but then you notice a zombie horde coming your way", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = _chatClient.SendMessageAsync("RUN");
            }
            else if (msg.IndexOf("megarace boost", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = _chatClient.SendMessageAsync("yes");
            }
            else if (msg.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = _chatClient.SendMessageAsync("CUT");
            }
            else if (msg.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = _chatClient.SendMessageAsync("LURE");
            }
            else if (msg.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = _chatClient.SendMessageAsync("CATCH");
            }
            else if (msg.IndexOf("God accidentally dropped an EPIC coin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg,
                    "I SHALL BRING THE EPIC TO THE COIN",
                    "MY PRECIOUS",
                    "WHAT IS EPIC? THIS COIN",
                    "YES! AN EPIC COIN",
                    "OPERATION: EPIC COIN");
            }
            else if (msg.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg,
                    "BACK OFF THIS IS MINE!!",
                    "HACOINA MATATA",
                    "THIS IS MINE",
                    "ALL THE COINS BELONG TO ME",
                    "GIMME DA MONEY",
                    "OPERATION: COINS");
            }
            else if (msg.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RespondFirstPresent(msg,
                    "YUP I WILL DO THAT",
                    "I WANT THAT",
                    "HEY EPIC NPC! I WANT TO TRADE WITH YOU",
                    "THAT SOUNDS LIKE AN OP BUSINESS",
                    "OWO ME!!!");
            }
            else if (msg.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("SUMMON");
            }
            else if (msg.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _ = SendAndEmitAsync("TIME TO FIGHT");
            }
<<<<<<< Updated upstream
=======

            _previousMessageText = msg;
>>>>>>> Stashed changes
        }

        private void HandleChangeWork(string message)
        {
            if (message.IndexOf("CHOP", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _work = "rpg chop";
                _ = SendAndEmitAsync("I am chopping treez");
            }
<<<<<<< Updated upstream
            catch { }
        }

        private static Task SafeDelay(int ms)
=======
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

                _ = _chatClient.SendMessageAsync(option);
                return;
            }
        }

        private async Task RunStartSequenceAsync(bool includeCd)
>>>>>>> Stashed changes
        {
            if (includeCd)
            {
                await SendAndEmitAsync("rpg cd");
                await SafeDelay(2001);
            }

            await SendAndEmitAsync(_hunt);
            await SafeDelay(2001);
            await SendAndEmitAsync(_work);
            await SafeDelay(2001);

            if (_area >= 4)
            {
                await SendAndEmitAsync(_farm);
            }
        }

        private Task SolveCaptchaAsync(string targetMessageId)
        {
            return _captchaSolver.TrySolveAsync(
                targetMessageId,
                _lastMessageId,
                _previousMessageId,
                SendAndEmitAsync,
                PauseWorkTimers,
                ResumeWorkTimers,
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
