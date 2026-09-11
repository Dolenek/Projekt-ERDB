using System;
using System.Windows.Threading;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class TrackedCommandScheduler
    {
        private DispatcherTimer GetTimer(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: return _dailyTimer;
                case TrackedCommandKind.Weekly: return _weeklyTimer;
                case TrackedCommandKind.CardHand: return _cardHandTimer;
                case TrackedCommandKind.Hunt: return _huntTimer;
                case TrackedCommandKind.Adventure: return _adventureTimer;
                case TrackedCommandKind.Training: return _trainingTimer;
                case TrackedCommandKind.Work: return _workTimer;
                case TrackedCommandKind.Farm: return _farmTimer;
                case TrackedCommandKind.Lootbox: return _lootboxTimer;
                default: throw UnknownKind(kind);
            }
        }

        private int GetBaseCooldownMs(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: return DailyCooldownMs;
                case TrackedCommandKind.Weekly: return WeeklyCooldownMs;
                case TrackedCommandKind.CardHand: return DailyCooldownMs;
                case TrackedCommandKind.Hunt: return _huntCooldown;
                case TrackedCommandKind.Adventure: return _adventureCooldown;
                case TrackedCommandKind.Training: return _trainingCooldown;
                case TrackedCommandKind.Work: return _workCooldown;
                case TrackedCommandKind.Farm: return _farmCooldown;
                case TrackedCommandKind.Lootbox: return _lootboxCooldown;
                default: throw UnknownKind(kind);
            }
        }

        private DateTime? GetDueUtc(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: return _dailyDueUtc;
                case TrackedCommandKind.Weekly: return _weeklyDueUtc;
                case TrackedCommandKind.CardHand: return _cardHandDueUtc;
                case TrackedCommandKind.Hunt: return _huntDueUtc;
                case TrackedCommandKind.Adventure: return _adventureDueUtc;
                case TrackedCommandKind.Training: return _trainingDueUtc;
                case TrackedCommandKind.Work: return _workDueUtc;
                case TrackedCommandKind.Farm: return _farmDueUtc;
                case TrackedCommandKind.Lootbox: return _lootboxDueUtc;
                default: throw UnknownKind(kind);
            }
        }

        private void SetDueUtc(TrackedCommandKind kind, DateTime? value)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: _dailyDueUtc = value; break;
                case TrackedCommandKind.Weekly: _weeklyDueUtc = value; break;
                case TrackedCommandKind.CardHand: _cardHandDueUtc = value; break;
                case TrackedCommandKind.Hunt: _huntDueUtc = value; break;
                case TrackedCommandKind.Adventure: _adventureDueUtc = value; break;
                case TrackedCommandKind.Training: _trainingDueUtc = value; break;
                case TrackedCommandKind.Work: _workDueUtc = value; break;
                case TrackedCommandKind.Farm: _farmDueUtc = value; break;
                case TrackedCommandKind.Lootbox: _lootboxDueUtc = value; break;
                default: throw UnknownKind(kind);
            }
        }

        private TimeSpan? GetPausedDelay(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: return _pausedDailyDelay;
                case TrackedCommandKind.Weekly: return _pausedWeeklyDelay;
                case TrackedCommandKind.CardHand: return _pausedCardHandDelay;
                case TrackedCommandKind.Hunt: return _pausedHuntDelay;
                case TrackedCommandKind.Adventure: return _pausedAdventureDelay;
                case TrackedCommandKind.Training: return _pausedTrainingDelay;
                case TrackedCommandKind.Work: return _pausedWorkDelay;
                case TrackedCommandKind.Farm: return _pausedFarmDelay;
                case TrackedCommandKind.Lootbox: return _pausedLootboxDelay;
                default: throw UnknownKind(kind);
            }
        }

        private void SetPausedDelay(TrackedCommandKind kind, TimeSpan? value)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: _pausedDailyDelay = value; break;
                case TrackedCommandKind.Weekly: _pausedWeeklyDelay = value; break;
                case TrackedCommandKind.CardHand: _pausedCardHandDelay = value; break;
                case TrackedCommandKind.Hunt: _pausedHuntDelay = value; break;
                case TrackedCommandKind.Adventure: _pausedAdventureDelay = value; break;
                case TrackedCommandKind.Training: _pausedTrainingDelay = value; break;
                case TrackedCommandKind.Work: _pausedWorkDelay = value; break;
                case TrackedCommandKind.Farm: _pausedFarmDelay = value; break;
                case TrackedCommandKind.Lootbox: _pausedLootboxDelay = value; break;
                default: throw UnknownKind(kind);
            }
        }

        private static ArgumentOutOfRangeException UnknownKind(TrackedCommandKind kind)
        {
            return new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown tracked command kind.");
        }
    }
}
