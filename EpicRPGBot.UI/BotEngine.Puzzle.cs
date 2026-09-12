using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private bool IsCurrentPuzzle(string messageId) =>
            _running && _guardIncidentTracker.IsActive &&
            string.Equals(_activeGuardMessageId, messageId, StringComparison.Ordinal);

        private async Task<bool> SendPuzzleAnswerAsync(string messageId, string answer, CancellationToken cancellationToken)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stopCancellation.Token))
            {
                await _sendGate.WaitAsync(linked.Token);
                try
                {
                    if (!IsCurrentPuzzle(messageId)) return false;
                    await RespectMinimumCommandGapAsync();
                    linked.Token.ThrowIfCancellationRequested();
                    if (!IsCurrentPuzzle(messageId)) return false;
                    if (!(_chatClient is Services.IPuzzleAnswerChatClient puzzleChat)) return false;
                    var sent = await puzzleChat.SendPuzzleAnswerOnceAsync(answer,
                        () => IsCurrentPuzzle(messageId), linked.Token);
                    if (sent)
                    {
                        _lastCommandSentUtc = DateTime.UtcNow;
                        OnCommandSent?.Invoke(answer, null);
                    }
                    return sent;
                }
                finally { _sendGate.Release(); }
            }
        }
    }
}
