using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Training;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class CooldownInitializationWorkflow
    {
        private const int TrainingConfirmationPollDelayMs = 250;
        private const int TrainingConfirmationTimeoutMs = 20000;
        private const int TrainingConfirmationScanCount = 20;

        private readonly TrainingPromptParser _trainingPromptParser = new TrainingPromptParser();

        private async Task<bool> TryAnswerTrainingPromptAsync(
            DiscordMessageSnapshot snapshot,
            Action<string> logInfo)
        {
            var resolution = _trainingPromptParser.Parse(snapshot);
            if (!resolution.IsTrainingPrompt)
            {
                logInfo?.Invoke("Inicialize: training reply was not recognized as a training prompt.");
                return false;
            }

            if (!resolution.IsResolved)
            {
                logInfo?.Invoke("Inicialize: training prompt could not be solved safely.");
                return false;
            }

            if (await TryClickButtonAsync(snapshot, resolution))
            {
                return await WaitForTrainingConfirmationAsync(snapshot.Id, logInfo);
            }

            if (string.IsNullOrWhiteSpace(resolution.AnswerText))
            {
                logInfo?.Invoke("Inicialize: training answer was empty after parsing.");
                return false;
            }

            if (await _chatClient.SendMessageAsync(resolution.AnswerText))
            {
                return await WaitForTrainingConfirmationAsync(snapshot.Id, logInfo);
            }

            logInfo?.Invoke("Inicialize: training answer failed to send.");
            return false;
        }

        private async Task<bool> TryClickButtonAsync(
            DiscordMessageSnapshot snapshot,
            TrainingPromptResolution resolution)
        {
            if (snapshot?.Buttons == null || resolution == null)
            {
                return false;
            }

            foreach (var button in snapshot.Buttons)
            {
                if (!LabelsMatch(button.Label, resolution.PreferredButtonLabel) &&
                    !LabelsMatch(button.Label, resolution.AnswerText))
                {
                    continue;
                }

                return await _chatClient.ClickMessageButtonAsync(
                    snapshot.Id,
                    button.RowIndex,
                    button.ColumnIndex);
            }

            return false;
        }

        private static bool LabelsMatch(string left, string right)
        {
            return string.Equals(
                NormalizeLabel(left),
                NormalizeLabel(right),
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> WaitForTrainingConfirmationAsync(
            string afterMessageId,
            Action<string> logInfo)
        {
            var cursorId = afterMessageId ?? string.Empty;
            var waitedMs = 0;
            while (waitedMs < TrainingConfirmationTimeoutMs)
            {
                await Task.Delay(TrainingConfirmationPollDelayMs);
                waitedMs += TrainingConfirmationPollDelayMs;

                var snapshots = await _chatClient.GetRecentMessagesAsync(TrainingConfirmationScanCount);
                var startIndex = ResolveStartIndex(snapshots, cursorId);
                for (var i = startIndex; snapshots != null && i < snapshots.Count; i++)
                {
                    var snapshot = snapshots[i];
                    if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.Id))
                    {
                        continue;
                    }

                    cursorId = snapshot.Id;
                    if (IsTrainingConfirmationMessage(snapshot.Text))
                    {
                        return true;
                    }
                }
            }

            logInfo?.Invoke(
                "Inicialize: training answer sent, but no 'Well done' confirmation was observed.");
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

        private static string NormalizeLabel(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var characters = value.Trim().Trim(':').ToCharArray();
            var output = new char[characters.Length];
            var count = 0;
            for (var index = 0; index < characters.Length; index++)
            {
                var current = characters[index];
                if (!char.IsLetterOrDigit(current))
                {
                    continue;
                }

                output[count++] = char.ToLowerInvariant(current);
            }

            return new string(output, 0, count);
        }
    }
}
