using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private bool IsCurrentCaptcha(string messageId) =>
            _running && _guardIncidentTracker.IsActive &&
            string.Equals(_activeGuardMessageId, messageId, StringComparison.Ordinal);

        private async Task<bool> SendCaptchaAnswerAsync(string messageId, string answer, CancellationToken cancellationToken)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopCancellation.Token))
            {
                await _sendGate.WaitAsync(linked.Token);
                try
                {
                    if (!IsCurrentCaptcha(messageId)) return false;
                    await RespectMinimumCommandGapAsync();
                    linked.Token.ThrowIfCancellationRequested();
                    if (!IsCurrentCaptcha(messageId)) return false;
                    if (!(_chatClient is Services.ICaptchaAnswerChatClient captchaChat)) return false;
                    var sent = await captchaChat.SendCaptchaAnswerOnceAsync(answer,
                        () => IsCurrentCaptcha(messageId), linked.Token);
                    if (sent)
                    {
                        _lastCommandSentUtc = DateTime.UtcNow;
                        OnCommandSent?.Invoke(answer);
                    }
                    return sent;
                }
                finally { _sendGate.Release(); }
            }
        }
    }
}
