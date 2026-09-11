using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class TrackedCommandScheduler
    {
        private const int DailyCooldownMs = 86400000;
        private const int WeeklyCooldownMs = 604800000;
        private const int MinimumDelayMs = 500;
        private const int RetryBufferMs = 1500;

        private sealed class PendingCommand
        {
            public PendingCommand(TrackedCommandKind kind)
            {
                Kind = kind;
                SentAtUtc = DateTime.UtcNow;
            }

            public TrackedCommandKind Kind { get; }
            public DateTime SentAtUtc { get; }
        }

        private readonly bool _farmEnabled;
        private bool _cardHandEnabled;
        private readonly int _huntCooldown;
        private readonly int _adventureCooldown;
        private readonly int _trainingCooldown;
        private readonly int _workCooldown;
        private readonly int _farmCooldown;
        private readonly int _lootboxCooldown;
        private readonly DispatcherTimer _dailyTimer;
        private readonly DispatcherTimer _weeklyTimer;
        private readonly DispatcherTimer _cardHandTimer;
        private readonly DispatcherTimer _huntTimer;
        private readonly DispatcherTimer _adventureTimer;
        private readonly DispatcherTimer _trainingTimer;
        private readonly DispatcherTimer _workTimer;
        private readonly DispatcherTimer _farmTimer;
        private readonly DispatcherTimer _lootboxTimer;
        private readonly List<PendingCommand> _pendingCommands = new List<PendingCommand>();

        private DateTime? _dailyDueUtc;
        private DateTime? _weeklyDueUtc;
        private DateTime? _cardHandDueUtc;
        private DateTime? _huntDueUtc;
        private DateTime? _adventureDueUtc;
        private DateTime? _trainingDueUtc;
        private DateTime? _workDueUtc;
        private DateTime? _farmDueUtc;
        private DateTime? _lootboxDueUtc;
        private TimeSpan? _pausedDailyDelay;
        private TimeSpan? _pausedWeeklyDelay;
        private TimeSpan? _pausedCardHandDelay;
        private TimeSpan? _pausedHuntDelay;
        private TimeSpan? _pausedAdventureDelay;
        private TimeSpan? _pausedTrainingDelay;
        private TimeSpan? _pausedWorkDelay;
        private TimeSpan? _pausedFarmDelay;
        private TimeSpan? _pausedLootboxDelay;

        public TrackedCommandScheduler(bool farmEnabled, int huntCooldown, int adventureCooldown, int trainingCooldown, int workCooldown, int farmCooldown, int lootboxCooldown, Func<TrackedCommandKind, Task> onTimerElapsed)
            : this(farmEnabled, true, huntCooldown, adventureCooldown, trainingCooldown, workCooldown, farmCooldown, lootboxCooldown, onTimerElapsed)
        {
        }

        public TrackedCommandScheduler(bool farmEnabled, bool cardHandEnabled, int huntCooldown, int adventureCooldown, int trainingCooldown, int workCooldown, int farmCooldown, int lootboxCooldown, Func<TrackedCommandKind, Task> onTimerElapsed)
        {
            if (onTimerElapsed == null) throw new ArgumentNullException(nameof(onTimerElapsed));

            _farmEnabled = farmEnabled;
            _cardHandEnabled = cardHandEnabled;
            _huntCooldown = huntCooldown;
            _adventureCooldown = adventureCooldown;
            _trainingCooldown = trainingCooldown;
            _workCooldown = workCooldown;
            _farmCooldown = farmCooldown;
            _lootboxCooldown = lootboxCooldown;
            _dailyTimer = CreateCommandTimer(DailyCooldownMs, () => onTimerElapsed(TrackedCommandKind.Daily));
            _weeklyTimer = CreateCommandTimer(WeeklyCooldownMs, () => onTimerElapsed(TrackedCommandKind.Weekly));
            _cardHandTimer = CreateCommandTimer(DailyCooldownMs, () => onTimerElapsed(TrackedCommandKind.CardHand));
            _huntTimer = CreateCommandTimer(huntCooldown, () => onTimerElapsed(TrackedCommandKind.Hunt));
            _adventureTimer = CreateCommandTimer(adventureCooldown, () => onTimerElapsed(TrackedCommandKind.Adventure));
            _trainingTimer = CreateCommandTimer(trainingCooldown, () => onTimerElapsed(TrackedCommandKind.Training));
            _workTimer = CreateCommandTimer(workCooldown, () => onTimerElapsed(TrackedCommandKind.Work));
            _farmTimer = CreateCommandTimer(farmCooldown, () => onTimerElapsed(TrackedCommandKind.Farm));
            _lootboxTimer = CreateCommandTimer(lootboxCooldown, () => onTimerElapsed(TrackedCommandKind.Lootbox));
        }

        public void RegisterPending(TrackedCommandKind kind)
        {
            _pendingCommands.RemoveAll(item => item.Kind == kind);
            _pendingCommands.Add(new PendingCommand(kind));
            SetDueUtc(kind, null);
        }

        public void ClearPending(TrackedCommandKind kind)
        {
            _pendingCommands.RemoveAll(item => item.Kind == kind);
        }

        public void Schedule(TrackedCommandKind kind, TimeSpan delay, bool isRunning)
        {
            if (!isRunning || (kind == TrackedCommandKind.Farm && !_farmEnabled) ||
                (kind == TrackedCommandKind.CardHand && !_cardHandEnabled))
            {
                return;
            }

            var actualDelay = delay < TimeSpan.FromMilliseconds(MinimumDelayMs)
                ? TimeSpan.FromMilliseconds(MinimumDelayMs)
                : delay;

            var timer = GetTimer(kind);
            timer.Interval = actualDelay;
            timer.Stop();
            timer.Start();
            SetDueUtc(kind, DateTime.UtcNow + actualDelay);
        }

        public void StopAll()
        {
            Stop(TrackedCommandKind.Daily);
            Stop(TrackedCommandKind.Weekly);
            Stop(TrackedCommandKind.CardHand);
            Stop(TrackedCommandKind.Hunt);
            Stop(TrackedCommandKind.Adventure);
            Stop(TrackedCommandKind.Training);
            Stop(TrackedCommandKind.Work);
            Stop(TrackedCommandKind.Farm);
            Stop(TrackedCommandKind.Lootbox);
        }

        public void ClearPending()
        {
            _pendingCommands.Clear();
        }

        public void PauseAll()
        {
            Pause(TrackedCommandKind.Daily);
            Pause(TrackedCommandKind.Weekly);
            Pause(TrackedCommandKind.CardHand);
            Pause(TrackedCommandKind.Hunt);
            Pause(TrackedCommandKind.Adventure);
            Pause(TrackedCommandKind.Training);
            Pause(TrackedCommandKind.Work);
            Pause(TrackedCommandKind.Farm);
            Pause(TrackedCommandKind.Lootbox);
        }

        public void ResumeAll(bool isRunning)
        {
            Resume(TrackedCommandKind.Daily, isRunning);
            Resume(TrackedCommandKind.Weekly, isRunning);
            Resume(TrackedCommandKind.CardHand, isRunning);
            Resume(TrackedCommandKind.Hunt, isRunning);
            Resume(TrackedCommandKind.Adventure, isRunning);
            Resume(TrackedCommandKind.Training, isRunning);
            Resume(TrackedCommandKind.Work, isRunning);
            Resume(TrackedCommandKind.Farm, isRunning);
            Resume(TrackedCommandKind.Lootbox, isRunning);
        }

        public void HandleResponse(Models.DiscordMessageSnapshot snapshot, bool isRunning)
        {
            if (!LooksLikeTrackedCommandResponse(snapshot))
            {
                return;
            }

            var message = snapshot.Text ?? string.Empty;
            var pending = TryMatchPending(message) ?? GetOldestPending();
            if (pending == null)
            {
                return;
            }

            _pendingCommands.Remove(pending);
            if (TryParseWaitAtLeast(message, out var retryDelay))
            {
                Schedule(pending.Kind, retryDelay + TimeSpan.FromMilliseconds(RetryBufferMs), isRunning);
                return;
            }

            Schedule(pending.Kind, TimeSpan.FromMilliseconds(GetBaseCooldownMs(pending.Kind)), isRunning);
        }

        public void SetCardHandEnabled(bool enabled, bool isRunning)
        {
            _cardHandEnabled = enabled;
            if (!enabled) Stop(TrackedCommandKind.CardHand);
        }

        private static DispatcherTimer CreateCommandTimer(int intervalMs, Func<Task> action)
        {
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };

            timer.Tick += async (sender, args) =>
            {
                timer.Stop();
                await action();
            };

            return timer;
        }

        private PendingCommand GetOldestPending()
        {
            PendingCommand oldest = null;
            foreach (var item in _pendingCommands)
            {
                if (oldest == null || item.SentAtUtc < oldest.SentAtUtc)
                {
                    oldest = item;
                }
            }

            return oldest;
        }

        private PendingCommand TryMatchPending(string message)
        {
            if (!TryInferKind(message, out var kind))
            {
                return null;
            }

            foreach (var item in _pendingCommands)
            {
                if (item.Kind == kind)
                {
                    return item;
                }
            }

            return null;
        }

        private void Stop(TrackedCommandKind kind)
        {
            GetTimer(kind).Stop();
            SetDueUtc(kind, null);
            SetPausedDelay(kind, null);
        }

        private void Pause(TrackedCommandKind kind)
        {
            var dueUtc = GetDueUtc(kind);
            var timer = GetTimer(kind);
            if (!timer.IsEnabled || !dueUtc.HasValue)
            {
                SetPausedDelay(kind, null);
                return;
            }

            var remaining = dueUtc.Value - DateTime.UtcNow;
            SetPausedDelay(kind, remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(MinimumDelayMs));
            timer.Stop();
            SetDueUtc(kind, null);
        }

        private void Resume(TrackedCommandKind kind, bool isRunning)
        {
            var paused = GetPausedDelay(kind);
            SetPausedDelay(kind, null);
            if (!paused.HasValue)
            {
                return;
            }

            Schedule(kind, paused.Value, isRunning);
        }

    }
}
