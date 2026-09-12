using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        internal bool CanOpenPets => !IsGuardIncidentActive && !IsInteractivePromptPending();

        internal async Task PauseForPetsAsync()
        {
            _scheduler.StopAll();
            _checkMessageTimer.Stop();
            await WaitForSendLaneIdleAsync();
            if (!CanOpenPets)
                throw new InvalidOperationException("An interactive command needs attention before opening Pets.");
            Stop();
        }

        // Exclusive UI workflows use the same lane, but a separate cancellation lifetime
        // after scheduled automation has been stopped and drained.
        internal async Task<ConfirmedCommandSendResult> SendPetCommandAsync(string command, CancellationToken token)
        {
            await _sendGate.WaitAsync(token);
            try
            {
                if (IsGuardIncidentActive || IsInteractivePromptPending())
                    throw new InvalidOperationException("Resolve the current interactive command before using Pets.");
                var delay = 1000 - (DateTime.UtcNow - _lastCommandSentUtc).TotalMilliseconds;
                if (delay > 0) await Task.Delay((int)Math.Ceiling(delay), token);
                var result = await _confirmedCommandSender.SendAsync(command, snapshot =>
                {
                    _lastCommandSentUtc = DateTime.UtcNow;
                    OnCommandSent?.Invoke(command, snapshot);
                }, token);
                return result;
            }
            finally
            {
                _lastCommandSentUtc = DateTime.UtcNow;
                _sendGate.Release();
            }
        }
    }
}
