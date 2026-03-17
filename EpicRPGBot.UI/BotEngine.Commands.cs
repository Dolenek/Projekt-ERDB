using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        public async Task<bool> SendImmediateAsync(string text)
        {
            var ok = await SendAndEmitAsync(text);
            return ok;
        }

        private async Task OnTrackedTimerElapsedAsync(TrackedCommandKind kind)
        {
            await SendTrackedCommandAsync(kind, GetCommandText(kind));
        }

        private async Task SendTrackedCommandAsync(TrackedCommandKind kind, string command)
        {
            if (!_running)
            {
                return;
            }

            try
            {
                var sent = await SendConfirmedCommandWithGlobalCooldownAsync(
                    command,
                    () =>
                    {
                        _scheduler.RegisterPending(kind);
                        OnCommandSent?.Invoke(command);
                    });

                if (sent)
                {
                    return;
                }

                _scheduler.ClearPending(kind);
                _scheduler.Schedule(kind, TimeSpan.FromSeconds(5), _running);
            }
            catch
            {
                _scheduler.ClearPending(kind);
                _scheduler.Schedule(kind, TimeSpan.FromSeconds(5), _running);
            }
        }

        private async Task SendQueuedCooldownSnapshotAsync()
        {
            try
            {
                if (!_running || Interlocked.CompareExchange(ref _queuedCooldownSnapshot, 1, 1) != 1)
                {
                    return;
                }

                await SendConfirmedCommandWithGlobalCooldownAsync("rpg cd", () => OnCommandSent?.Invoke("rpg cd"));
            }
            finally
            {
                Interlocked.Exchange(ref _queuedCooldownSnapshot, 0);
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
                case TrackedCommandKind.Farm: return _farm;
                default: return _lootbox;
            }
        }

        private async Task<bool> SendAndEmitAsync(string text)
        {
            if (ConfirmedCommandSender.RequiresReplyConfirmation(text))
            {
                return await SendConfirmedCommandWithGlobalCooldownAsync(text, () => OnCommandSent?.Invoke(text));
            }

            var ok = await SendRawWithGlobalCooldownAsync(text);
            if (ok)
            {
                OnCommandSent?.Invoke(text);
            }

            return ok;
        }

        private async Task<bool> SendConfirmedCommandWithGlobalCooldownAsync(string text, Action onOutgoingRegistered)
        {
            await _sendGate.WaitAsync();
            try
            {
                await RespectMinimumCommandGapAsync();

                var result = await _confirmedCommandSender.SendAsync(
                    text,
                    snapshot =>
                    {
                        _lastCommandSentUtc = DateTime.UtcNow;
                        onOutgoingRegistered?.Invoke();
                    });

                if (result.IsConfirmed)
                {
                    await ProcessIncomingMessagesAsync();
                    return true;
                }

                return false;
            }
            finally
            {
                _sendGate.Release();
            }
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
    }
}
