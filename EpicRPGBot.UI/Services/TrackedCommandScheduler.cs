using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace EpicRPGBot.UI.Services
{
    public enum TrackedCommandKind
    {
        Hunt,
        Adventure,
        Work,
        Farm
    }

    public sealed class TrackedCommandScheduler
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

        private readonly int _area;
        private readonly int _huntCooldown;
        private readonly int _adventureCooldown;
        private readonly int _workCooldown;
        private readonly int _farmCooldown;
        private readonly DispatcherTimer _huntTimer;
        private readonly DispatcherTimer _adventureTimer;
        private readonly DispatcherTimer _workTimer;
        private readonly DispatcherTimer _farmTimer;
        private readonly List<PendingCommand> _pendingCommands = new List<PendingCommand>();

        private DateTime? _huntDueUtc;
        private DateTime? _adventureDueUtc;
        private DateTime? _workDueUtc;
        private DateTime? _farmDueUtc;
        private TimeSpan? _pausedHuntDelay;
        private TimeSpan? _pausedAdventureDelay;
        private TimeSpan? _pausedWorkDelay;
        private TimeSpan? _pausedFarmDelay;

        public TrackedCommandScheduler(int area, int huntCooldown, int adventureCooldown, int workCooldown, int farmCooldown, Func<TrackedCommandKind, Task> onTimerElapsed)
        {
            if (onTimerElapsed == null) throw new ArgumentNullException(nameof(onTimerElapsed));

            _area = area;
            _huntCooldown = huntCooldown;
            _adventureCooldown = adventureCooldown;
            _workCooldown = workCooldown;
            _farmCooldown = farmCooldown;
            _huntTimer = CreateCommandTimer(huntCooldown, () => onTimerElapsed(TrackedCommandKind.Hunt));
            _adventureTimer = CreateCommandTimer(adventureCooldown, () => onTimerElapsed(TrackedCommandKind.Adventure));
            _workTimer = CreateCommandTimer(workCooldown, () => onTimerElapsed(TrackedCommandKind.Work));
            _farmTimer = CreateCommandTimer(farmCooldown, () => onTimerElapsed(TrackedCommandKind.Farm));
        }

        public void RegisterPending(TrackedCommandKind kind)
        {
            _pendingCommands.RemoveAll(item => item.Kind == kind);
            _pendingCommands.Add(new PendingCommand(kind));
            SetDueUtc(kind, null);
        }

        public void Schedule(TrackedCommandKind kind, TimeSpan delay, bool isRunning)
        {
            if (!isRunning || (kind == TrackedCommandKind.Farm && _area < 4))
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
            Stop(TrackedCommandKind.Work);
            Stop(TrackedCommandKind.Farm);
        }

        public void ClearPending()
        {
            _pendingCommands.Clear();
        }

        public void PauseAll()
        {
            Pause(TrackedCommandKind.Hunt);
            Pause(TrackedCommandKind.Adventure);
            Pause(TrackedCommandKind.Work);
            Pause(TrackedCommandKind.Farm);
        }

        public void ResumeAll(bool isRunning)
        {
            Resume(TrackedCommandKind.Hunt, isRunning);
            Resume(TrackedCommandKind.Adventure, isRunning);
            Resume(TrackedCommandKind.Work, isRunning);
            Resume(TrackedCommandKind.Farm, isRunning);
        }

        public void HandleResponse(string message, bool isRunning)
        {
            if (!LooksLikeTrackedCommandResponse(message))
            {
                return;
            }

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
                case TrackedCommandKind.Work: return _workTimer;
                default: return _farmTimer;
            }
        }

        private int GetBaseCooldownMs(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _huntCooldown;
                case TrackedCommandKind.Adventure: return _adventureCooldown;
                case TrackedCommandKind.Work: return _workCooldown;
                default: return _farmCooldown;
            }
        }

        private DateTime? GetDueUtc(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _huntDueUtc;
                case TrackedCommandKind.Adventure: return _adventureDueUtc;
                case TrackedCommandKind.Work: return _workDueUtc;
                default: return _farmDueUtc;
            }
        }

        private void SetDueUtc(TrackedCommandKind kind, DateTime? value)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: _huntDueUtc = value; break;
                case TrackedCommandKind.Adventure: _adventureDueUtc = value; break;
                case TrackedCommandKind.Work: _workDueUtc = value; break;
                default: _farmDueUtc = value; break;
            }
        }

        private TimeSpan? GetPausedDelay(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: return _pausedHuntDelay;
                case TrackedCommandKind.Adventure: return _pausedAdventureDelay;
                case TrackedCommandKind.Work: return _pausedWorkDelay;
                default: return _pausedFarmDelay;
            }
        }

        private void SetPausedDelay(TrackedCommandKind kind, TimeSpan? value)
        {
            switch (kind)
            {
                case TrackedCommandKind.Hunt: _pausedHuntDelay = value; break;
                case TrackedCommandKind.Adventure: _pausedAdventureDelay = value; break;
                case TrackedCommandKind.Work: _pausedWorkDelay = value; break;
                default: _pausedFarmDelay = value; break;
            }
        }

        private static bool LooksLikeTrackedCommandResponse(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return message.IndexOf("EPIC GUARD", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("A LOOTBOX SUMMONING HAS", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("A LEGENDARY BOSS JUST SPAWNED", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("AN EPIC TREE HAS JUST GROWN", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("A MEGALODON HAS SPAWNED IN THE RIVER", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("IT'S RAINING COINS", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("God accidentally dropped", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("OOPS! God accidentally dropped", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("EPIC NPC: I have a special trade today!", StringComparison.OrdinalIgnoreCase) < 0 &&
                   message.IndexOf("rpg ", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool TryInferKind(string message, out TrackedCommandKind kind)
        {
            var msg = (message ?? string.Empty).ToLowerInvariant();

            if (msg.Contains("looked around") ||
                msg.Contains("found and killed") ||
                msg.Contains("defenseless monster") ||
                msg.Contains("zombie horde"))
            {
                kind = TrackedCommandKind.Hunt;
                return true;
            }

            if (msg.Contains("adventure") ||
                msg.Contains("you went") ||
                msg.Contains("went exploring") ||
                msg.Contains("found a cave") ||
                msg.Contains("got lost") ||
                msg.Contains("adv h"))
            {
                kind = TrackedCommandKind.Adventure;
                return true;
            }

            if (msg.Contains("is chopping") ||
                msg.Contains("is fishing") ||
                msg.Contains("is picking up") ||
                msg.Contains("is mining") ||
                msg.Contains("chainsaw") ||
                msg.Contains("bowsaw") ||
                msg.Contains("axe") ||
                msg.Contains("wooden log") ||
                msg.Contains("normie fish") ||
                msg.Contains("lootbox summoning") ||
                msg.Contains("mermaid hair"))
            {
                kind = TrackedCommandKind.Work;
                return true;
            }

            if (msg.Contains("farm") ||
                msg.Contains("working on the fields") ||
                msg.Contains("carrot") ||
                msg.Contains("potato") ||
                msg.Contains("bread"))
            {
                kind = TrackedCommandKind.Farm;
                return true;
            }

            kind = TrackedCommandKind.Hunt;
            return false;
        }

        private static bool TryParseWaitAtLeast(string message, out TimeSpan delay)
        {
            delay = TimeSpan.Zero;

            var match = Regex.Match(message ?? string.Empty, @"wait at least\s+((?:\d+\s*[dhms]\s*)+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            var total = TimeSpan.Zero;
            var units = Regex.Matches(match.Groups[1].Value, @"(\d+)\s*([dhms])", RegexOptions.IgnoreCase);
            foreach (Match unit in units)
            {
                var value = int.Parse(unit.Groups[1].Value);
                switch (unit.Groups[2].Value.ToLowerInvariant())
                {
                    case "d": total += TimeSpan.FromDays(value); break;
                    case "h": total += TimeSpan.FromHours(value); break;
                    case "m": total += TimeSpan.FromMinutes(value); break;
                    case "s": total += TimeSpan.FromSeconds(value); break;
                }
            }

            delay = total <= TimeSpan.Zero ? TimeSpan.FromSeconds(1) : total;
            return true;
        }
    }
}
