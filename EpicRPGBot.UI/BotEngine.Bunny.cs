using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Bunny;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private bool TryHandleBunnyPrompt(DiscordMessageSnapshot snapshot)
        {
            var author = snapshot?.Author ?? string.Empty;
            var text = snapshot?.Text ?? string.Empty;
            if (author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var parseResult = _bunnyPromptParser.Parse(snapshot?.RenderedText ?? text);
            if (!parseResult.IsBunnyPrompt)
            {
                return false;
            }

            if (!_interactivePromptGate.TryBeginBunny())
            {
                return true;
            }

            var plan = _bunnyCatchPlanBuilder.Build(parseResult);
            if (plan.UsedFallback)
            {
                RaiseBunnyAlert(plan.Summary);
            }

            _ = AnswerBunnyPromptAsync(plan);
            return true;
        }

        private async Task AnswerBunnyPromptAsync(BunnyCatchPlan plan)
        {
            var sendGateHeld = false;
            try
            {
                await _sendGate.WaitAsync(_stopCancellation.Token);
                sendGateHeld = true;

                if (string.IsNullOrWhiteSpace(plan?.ReplyText))
                {
                    RaiseBunnyAlert("Bunny reply was empty and could not be sent.");
                    return;
                }

                await RespectMinimumCommandGapAsync();
                var sent = await _chatClient.SendMessageAsync(plan.ReplyText, _stopCancellation.Token);
                if (!sent)
                {
                    RaiseBunnyAlert("Bunny reply failed to send: " + plan.Summary);
                    return;
                }

                _lastCommandSentUtc = DateTime.UtcNow;
                ReportBunnyInfo(plan.Summary);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                RaiseBunnyAlert("Bunny prompt handling failed: " + ex.Message);
            }
            finally
            {
                _interactivePromptGate.EndBunny();
                if (sendGateHeld)
                {
                    _sendGate.Release();
                }
            }
        }

        private void ReportBunnyInfo(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            OnBunnyInfo?.Invoke(message);
        }

        private void RaiseBunnyAlert(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            OnBunnyAlert?.Invoke(message);
        }
    }
}
