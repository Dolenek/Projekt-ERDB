using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Training;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private const int TrainingConfirmationPollDelayMs = 250;
        private const int TrainingConfirmationTimeoutMs = 20000;
        private const int TrainingConfirmationScanCount = 20;

        private bool TryHandleTrainingPrompt(DiscordMessageSnapshot snapshot)
        {
            var author = snapshot?.Author ?? string.Empty;
            var text = snapshot?.Text ?? string.Empty;
            if (author.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("EPIC RPG", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var resolution = _trainingPromptParser.Parse(snapshot);
            if (!resolution.IsTrainingPrompt)
            {
                return false;
            }

            if (!resolution.IsResolved)
            {
                RaiseTrainingAlert(resolution.Summary);
                return true;
            }

            if (Interlocked.Exchange(ref _trainingConfirmationPending, 1) == 1)
            {
                return true;
            }

            _ = AnswerTrainingPromptAsync(snapshot, resolution);
            return true;
        }

        private async Task AnswerTrainingPromptAsync(DiscordMessageSnapshot snapshot, TrainingPromptResolution resolution)
        {
            if (snapshot == null || resolution == null || !resolution.IsResolved)
            {
                return;
            }

            try
            {
                await _sendGate.WaitAsync(_stopCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                if (!await SendTrainingAnswerAsync(snapshot, resolution))
                {
                    return;
                }

                if (await WaitForTrainingConfirmationAsync(snapshot.Id, _stopCancellation.Token))
                {
                    return;
                }

                RaiseTrainingAlert($"Training prompt confirmation timed out: {resolution.Summary}");
            }
            catch (Exception ex)
            {
                RaiseTrainingAlert("Training prompt handling failed: " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _trainingConfirmationPending, 0);
                _sendGate.Release();
            }
        }

        private async Task<bool> SendTrainingAnswerAsync(DiscordMessageSnapshot snapshot, TrainingPromptResolution resolution)
        {
            if (await TryClickTrainingButtonAsync(snapshot, resolution))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(resolution.AnswerText))
            {
                RaiseTrainingAlert($"Training prompt answer failed to send: {resolution.Summary}");
                return false;
            }

            await RespectMinimumCommandGapAsync();
            var sent = await _chatClient.SendMessageAsync(resolution.AnswerText, _stopCancellation.Token);
            if (sent)
            {
                _lastCommandSentUtc = DateTime.UtcNow;
                return true;
            }

            RaiseTrainingAlert($"Training prompt answer failed to send: {resolution.Summary}");
            return false;
        }

        private async Task<bool> TryClickTrainingButtonAsync(DiscordMessageSnapshot snapshot, TrainingPromptResolution resolution)
        {
            var button = snapshot?.Buttons?
                .FirstOrDefault(candidate => LabelsMatch(candidate.Label, resolution.PreferredButtonLabel)) ??
                snapshot?.Buttons?
                .FirstOrDefault(candidate => LabelsMatch(candidate.Label, resolution.AnswerText));
            if (button == null)
            {
                return false;
            }

            return await _chatClient.ClickMessageButtonAsync(snapshot.Id, button.RowIndex, button.ColumnIndex, _stopCancellation.Token);
        }

        private async Task<bool> WaitForTrainingConfirmationAsync(string afterMessageId, CancellationToken cancellationToken)
        {
            var cursorId = afterMessageId ?? string.Empty;
            var waitedMs = 0;
            while (waitedMs < TrainingConfirmationTimeoutMs)
            {
                await SafeDelay(TrainingConfirmationPollDelayMs, cancellationToken);
                waitedMs += TrainingConfirmationPollDelayMs;

                var snapshots = await _chatClient.GetRecentMessagesAsync(TrainingConfirmationScanCount);
                if (snapshots == null || snapshots.Count == 0)
                {
                    continue;
                }

                var startIndex = ResolveStartIndex(snapshots, cursorId);
                for (var i = startIndex; i < snapshots.Count; i++)
                {
                    var candidate = snapshots[i];
                    if (candidate == null || string.IsNullOrWhiteSpace(candidate.Id))
                    {
                        continue;
                    }

                    cursorId = candidate.Id;
                    if (IsTrainingConfirmationMessage(candidate.Text))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static int ResolveStartIndex(
            System.Collections.Generic.IReadOnlyList<DiscordMessageSnapshot> snapshots,
            string cursorId)
        {
            if (snapshots == null || snapshots.Count == 0 || string.IsNullOrWhiteSpace(cursorId))
            {
                return 0;
            }

            for (var i = 0; i < snapshots.Count; i++)
            {
                if (string.Equals(snapshots[i]?.Id, cursorId, StringComparison.Ordinal))
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static bool IsTrainingConfirmationMessage(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                message.IndexOf("Well done", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RaiseTrainingAlert(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            OnTrainingAlert?.Invoke(message);
        }

        private static bool LabelsMatch(string left, string right)
        {
            return string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Trim()
                .Trim(':')
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }
    }
}
