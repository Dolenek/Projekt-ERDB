using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        public void UpdateHuntAndAdventureCommands(bool useHardcore)
        {
            _hunt = HuntAdventureCommandCatalog.ResolveHunt(useHardcore);
            _adventure = HuntAdventureCommandCatalog.ResolveAdventure(useHardcore);
        }

        public void UpdateHealAfterHuntAndAdventure(bool enabled)
        {
            _healAfterHuntAndAdventure = enabled;
        }

        private async Task OnTrackedTimerElapsedAsync(TrackedCommandKind kind)
        {
            if (kind == TrackedCommandKind.CardHand)
            {
                await RunCardHandAsync();
                return;
            }

            await SendTrackedCommandAsync(kind, GetCommandText(kind));
        }

        private string GetCommandText(TrackedCommandKind kind)
        {
            switch (kind)
            {
                case TrackedCommandKind.Daily: return "rpg daily";
                case TrackedCommandKind.Weekly: return "rpg weekly";
                case TrackedCommandKind.CardHand: return "rpg card hand";
                case TrackedCommandKind.Hunt: return _hunt;
                case TrackedCommandKind.Adventure: return _adventure;
                case TrackedCommandKind.Training: return _training;
                case TrackedCommandKind.Work: return _work;
                case TrackedCommandKind.Farm: return _farm;
                case TrackedCommandKind.Lootbox: return _lootbox;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private async Task SendTrackedCommandAsync(TrackedCommandKind kind, string command)
        {
            if (!_running)
            {
                return;
            }

            if (IsGuardIncidentActive)
            {
                ReportSolverInfo($"Skipped scheduled command '{command}' while EPIC GUARD incident is active.");
                _scheduler.Schedule(kind, TimeSpan.FromSeconds(5), _running);
                return;
            }

            try
            {
                var sent = await SendConfirmedCommandWithGlobalCooldownAsync(
                    command,
                    snapshot =>
                    {
                        _scheduler.RegisterPending(kind);
                        OnCommandSent?.Invoke(command, snapshot);
                    });

                if (sent)
                {
                    await SendConfiguredHealAsync(command);
                    return;
                }

                ScheduleTrackedRetry(kind);
            }
            catch
            {
                ScheduleTrackedRetry(kind);
            }
        }

        private void ScheduleTrackedRetry(TrackedCommandKind kind)
        {
            _scheduler.ClearPending(kind);
            if (_running)
            {
                _scheduler.Schedule(kind, TimeSpan.FromSeconds(5), true);
            }
        }

        private async Task SendConfiguredHealAsync(string completedCommand)
        {
            if (!_running || !HuntAdventureCommandCatalog.ShouldSendHealAfter(
                completedCommand,
                _healAfterHuntAndAdventure))
            {
                return;
            }

            await SendAndEmitAsync("rpg heal");
        }
    }
}
