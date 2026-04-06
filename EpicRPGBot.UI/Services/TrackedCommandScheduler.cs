using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EpicRPGBot.UI.Services
{
    public enum TrackedCommandKind
    {
        Hunt,
        Adventure,
        Training,
        Work,
        Farm,
        Lootbox
    }

    public sealed partial class TrackedCommandScheduler
    {
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
        private readonly int _huntCooldown;
        private readonly int _adventureCooldown;
        private readonly int _trainingCooldown;
        private readonly int _workCooldown;
        private readonly int _farmCooldown;
        private readonly int _lootboxCooldown;
        private readonly DispatcherTimer _huntTimer;
        private readonly DispatcherTimer _adventureTimer;
        private readonly DispatcherTimer _trainingTimer;
        private readonly DispatcherTimer _workTimer;
        private readonly DispatcherTimer _farmTimer;
        private readonly DispatcherTimer _lootboxTimer;
        private readonly List<PendingCommand> _pendingCommands = new List<PendingCommand>();

        private DateTime? _huntDueUtc;
        private DateTime? _adventureDueUtc;
        private DateTime? _trainingDueUtc;
        private DateTime? _workDueUtc;
        private DateTime? _farmDueUtc;
        private DateTime? _lootboxDueUtc;
        private TimeSpan? _pausedHuntDelay;
        private TimeSpan? _pausedAdventureDelay;
        private TimeSpan? _pausedTrainingDelay;
        private TimeSpan? _pausedWorkDelay;
        private TimeSpan? _pausedFarmDelay;
        private TimeSpan? _pausedLootboxDelay;

        public TrackedCommandScheduler(bool farmEnabled, int huntCooldown, int adventureCooldown, int trainingCooldown, int workCooldown, int farmCooldown, int lootboxCooldown, Func<TrackedCommandKind, Task> onTimerElapsed)
        {
            if (onTimerElapsed == null) throw new ArgumentNullException(nameof(onTimerElapsed));

            _farmEnabled = farmEnabled;
            _huntCooldown = huntCooldown;
            _adventureCooldown = adventureCooldown;
            _trainingCooldown = trainingCooldown;
            _workCooldown = workCooldown;
            _farmCooldown = farmCooldown;
            _lootboxCooldown = lootboxCooldown;
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
            if (!isRunning || (kind == TrackedCommandKind.Farm && !_farmEnabled))
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
            Pause(TrackedCommandKind.Hunt);
            Pause(TrackedCommandKind.Adventure);
            Pause(TrackedCommandKind.Training);
            Pause(TrackedCommandKind.Work);
            Pause(TrackedCommandKind.Farm);
            Pause(TrackedCommandKind.Lootbox);
        }

        public void ResumeAll(bool isRunning)
        {
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

        private DispatcherTimer GetTimer(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _huntTimer;
                case TrackedCommandKind.Adventure: return _adventureTimer;
                case TrackedCommandKind.Training: return _trainingTimer;
                case TrackedCommandKind.Work: return _workTimer;
                case TrackedCommandKind.Farm: return _farmTimer;
                default: return _lootboxTimer;
            }
        }

        private int GetBaseCooldownMs(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _huntCooldown;
                case TrackedCommandKind.Adventure: return _adventureCooldown;
                case TrackedCommandKind.Training: return _trainingCooldown;
                case TrackedCommandKind.Work: return _workCooldown;
                case TrackedCommandKind.Farm: return _farmCooldown;
                default: return _lootboxCooldown;
            }
        }

        private DateTime? GetDueUtc(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _huntDueUtc;
                case TrackedCommandKind.Adventure: return _adventureDueUtc;
                case TrackedCommandKind.Training: return _trainingDueUtc;
                case TrackedCommandKind.Work: return _workDueUtc;
                case TrackedCommandKind.Farm: return _farmDueUtc;
                default: return _lootboxDueUtc;
            }
        }

        private void SetDueUtc(TrackedCommandKind kind, DateTime? value)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: _huntDueUtc = value; break;
                case TrackedCommandKind.Adventure: _adventureDueUtc = value; break;
                case TrackedCommandKind.Training: _trainingDueUtc = value; break;
                case TrackedCommandKind.Work: _workDueUtc = value; break;
                case TrackedCommandKind.Farm: _farmDueUtc = value; break;
                default: _lootboxDueUtc = value; break;
            }
        }

        private TimeSpan? GetPausedDelay(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _pausedHuntDelay;
                case TrackedCommandKind.Adventure: return _pausedAdventureDelay;
                case TrackedCommandKind.Training: return _pausedTrainingDelay;
                case TrackedCommandKind.Work: return _pausedWorkDelay;
                case TrackedCommandKind.Farm: return _pausedFarmDelay;
                default: return _pausedLootboxDelay;
            }
        }

        private void SetPausedDelay(TrackedCommandKind kind, TimeSpan? value)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: _pausedHuntDelay = value; break;
                case TrackedCommandKind.Adventure: _pausedAdventureDelay = value; break;
                case TrackedCommandKind.Training: _pausedTrainingDelay = value; break;
                case TrackedCommandKind.Work: _pausedWorkDelay = value; break;
                case TrackedCommandKind.Farm: _pausedFarmDelay = value; break;
                default: _pausedLootboxDelay = value; break;
            }
        }

    }
}
