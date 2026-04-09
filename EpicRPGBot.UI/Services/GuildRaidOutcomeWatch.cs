using System;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public enum GuildRaidOutcomeState
    {
        Inactive,
        Waiting,
        Guarded,
        Confirmed
    }

    public sealed class GuildRaidOutcomeWatch
    {
        private bool _armed;

        public bool IsArmed => _armed;

        public void Arm()
        {
            _armed = true;
        }

        public void Reset()
        {
            _armed = false;
        }

        public GuildRaidOutcomeState Observe(DiscordMessageSnapshot snapshot)
        {
            if (!_armed)
            {
                return GuildRaidOutcomeState.Inactive;
            }

            if (GuildRaidResponseClassifier.IsGuardPrompt(snapshot))
            {
                _armed = false;
                return GuildRaidOutcomeState.Guarded;
            }

            if (GuildRaidResponseClassifier.IsRaidConfirmation(snapshot))
            {
                _armed = false;
                return GuildRaidOutcomeState.Confirmed;
            }

            return GuildRaidOutcomeState.Waiting;
        }
    }
}
