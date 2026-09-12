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
            var text = snapshot?.Text ?? string.Empty;
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
            var messageReference = DiscordMessageReference.FromSnapshot(snapshot);
            if (plan.UsedFallback)
            {
                RaiseBunnyAlert(plan.Summary, messageReference);
            }

            _ = AnswerBunnyPromptAsync(plan, messageReference);
            return true;
        }

        private async Task AnswerBunnyPromptAsync(
            BunnyCatchPlan plan,
            DiscordMessageReference messageReference)
        {
            var sendGateHeld = false;
            try
            {
                await _sendGate.WaitAsync(_stopCancellation.Token);
                sendGateHeld = true;

                if (string.IsNullOrWhiteSpace(plan?.ReplyText))
                {
                    RaiseBunnyAlert("Bunny reply was empty and could not be sent.", messageReference);
                    return;
                }

                await RespectMinimumCommandGapAsync();
                var sent = await _chatClient.SendMessageAsync(plan.ReplyText, _stopCancellation.Token);
                if (!sent)
                {
                    RaiseBunnyAlert("Bunny reply failed to send: " + plan.Summary, messageReference);
                    return;
                }

                _lastCommandSentUtc = DateTime.UtcNow;
                ReportBunnyInfo(plan.Summary, messageReference);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                RaiseBunnyAlert("Bunny prompt handling failed: " + ex.Message, messageReference);
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

        private void ReportBunnyInfo(string message, DiscordMessageReference messageReference)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            OnBunnyInfo?.Invoke(message, messageReference);
        }

        private void RaiseBunnyAlert(string message, DiscordMessageReference messageReference)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            OnBunnyAlert?.Invoke(message, messageReference);
        }
    }
}
