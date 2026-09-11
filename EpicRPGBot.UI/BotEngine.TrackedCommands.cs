using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
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
    }
}
