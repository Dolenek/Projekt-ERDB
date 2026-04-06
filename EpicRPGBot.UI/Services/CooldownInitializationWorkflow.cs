using System;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Training;

namespace EpicRPGBot.UI.Services
{
    public sealed class CooldownInitializationWorkflow
    {
        private const int ActionDelayMs = 2000;
        private const int AfterCdDelayMs = 1000;
        private const int ExtraLagMs = 1000;
        private const int OverheadMs = ActionDelayMs + AfterCdDelayMs + ExtraLagMs;
        private const int BetweenCommandsMs = 3000;
        private const int TrainingConfirmationPollDelayMs = 250;
        private const int TrainingConfirmationTimeoutMs = 20000;
        private const int TrainingConfirmationScanCount = 20;

        private readonly IDiscordChatClient _chatClient;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly CooldownTracker _tracker;
        private readonly AppSettingsService _settingsService;
        private readonly TrainingPromptParser _trainingPromptParser = new TrainingPromptParser();

        public CooldownInitializationWorkflow(
            IDiscordChatClient chatClient,
            CooldownTracker tracker,
            AppSettingsService settingsService)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            _confirmedCommandSender = new ConfirmedCommandSender(_chatClient);
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        }

        public async Task RunAsync(
            Action<string> logInfo,
            int adventureDefaultMs,
            int trainingDefaultMs,
            int workDefaultMs,
            int farmDefaultMs,
            int lootboxDefaultMs)
        {
            logInfo?.Invoke("Inicialize sequence started");
            var currentSettings = _settingsService.Current;
            var configuredArea = currentSettings.GetAreaOrDefault(10);
            var workAction = currentSettings.ResolveWorkCommandForArea(configuredArea);

            var openingSnapshot = await CaptureOpeningSnapshotAsync(logInfo);
            if (openingSnapshot == null)
            {
                logInfo?.Invoke("Inicialize aborted: failed to parse the opening 'rpg cd' snapshot.");
                return;
            }

            var steps = new[]
            {
                new InitializationStep("hunt", "rpg hunt h", 61000),
                new InitializationStep("adventure", "rpg adv h", adventureDefaultMs),
                new InitializationStep("training", "rpg tr", trainingDefaultMs),
                new InitializationStep("farm", "rpg farm", farmDefaultMs),
                new InitializationStep("work", workAction, workDefaultMs),
                new InitializationStep("lootbox", "rpg buy ed lb", lootboxDefaultMs)
            };

            for (var i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (HasRemainingCooldown(openingSnapshot, step.Canonical))
                {
                    logInfo?.Invoke($"Inicialize: {step.Canonical} skipped (already on cooldown in opening snapshot)");
                    continue;
                }

                var initialized = await InitializeOneAsync(step.Canonical, step.Action, step.DefaultMs, logInfo);
                if (initialized && i < steps.Length - 1)
                {
                    await Task.Delay(BetweenCommandsMs);
                }
            }

            logInfo?.Invoke("Inicialize sequence finished");
        }

        private async Task<TrackedCooldownSnapshot> CaptureOpeningSnapshotAsync(Action<string> logInfo)
        {
            var result = await _confirmedCommandSender.SendAsync("rpg cd");
            if (!result.IsConfirmed || !_tracker.ApplyMessage(result.ReplyMessage?.Text))
            {
                logInfo?.Invoke("Inicialize: opening 'rpg cd' snapshot was not usable.");
                return null;
            }

            return _tracker.GetTrackedSnapshot();
        }

        private async Task<bool> InitializeOneAsync(string canonical, string action, int defaultMs, Action<string> logInfo)
        {
            logInfo?.Invoke($"Inicialize: {canonical} via '{action}'");

            var actionResult = await _confirmedCommandSender.SendAsync(action);
            if (!actionResult.IsConfirmed)
            {
                logInfo?.Invoke($"Inicialize: failed to send '{action}'");
                return false;
            }

            if (LooksLikeCooldownReply(actionResult.ReplyMessage?.Text))
            {
                logInfo?.Invoke($"Inicialize: {canonical} skipped (command reply reported an active cooldown)");
                return false;
            }

            if (canonical == "training" &&
                !await TryAnswerTrainingPromptAsync(actionResult.ReplyMessage, logInfo))
            {
                return false;
            }

            await Task.Delay(ActionDelayMs);

            var cdResult = await _confirmedCommandSender.SendAsync("rpg cd");
            if (!cdResult.IsConfirmed)
            {
                logInfo?.Invoke("Inicialize: failed to refresh 'rpg cd' after action.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(cdResult.ReplyMessage?.Text))
            {
                _tracker.ApplyMessage(cdResult.ReplyMessage.Text);
            }

            var baseMs = defaultMs;
            var remaining = _tracker.GetRemaining(canonical);
            if (remaining.HasValue)
            {
                baseMs = (int)Math.Max(0, remaining.Value.TotalMilliseconds) + OverheadMs;
            }

            SaveCooldownSetting(canonical, baseMs);
            logInfo?.Invoke($"Inicialize: {canonical} cooldown set to {baseMs} ms (saved)");
            return true;
        }

        private void SaveCooldownSetting(string canonical, int milliseconds)
        {
            var value = milliseconds.ToString();
            var current = _settingsService.Current;
            if (canonical == "hunt")
            {
                _settingsService.Save(current.WithHuntMs(value));
                return;
            }

            if (canonical == "adventure")
            {
                _settingsService.Save(current.WithAdventureMs(value));
                return;
            }

            if (canonical == "training")
            {
                _settingsService.Save(current.WithTrainingMs(value));
                return;
            }

            if (canonical == "work")
            {
                _settingsService.Save(current.WithWorkMs(value));
                return;
            }

            if (canonical == "farm")
            {
                _settingsService.Save(current.WithFarmMs(value));
                return;
            }

            if (canonical == "lootbox")
            {
                _settingsService.Save(current.WithLootboxMs(value));
            }
        }

        private static bool HasRemainingCooldown(TrackedCooldownSnapshot snapshot, string canonical)
        {
            var remaining = canonical == "hunt" ? snapshot.Hunt :
                canonical == "adventure" ? snapshot.Adventure :
                canonical == "training" ? snapshot.Training :
                canonical == "work" ? snapshot.Work :
                canonical == "farm" ? snapshot.Farm :
                canonical == "lootbox" ? snapshot.Lootbox :
                null;

            return remaining.HasValue && remaining.Value > TimeSpan.Zero;
        }

        private static bool LooksLikeCooldownReply(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                   message.IndexOf("wait at least", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<bool> TryAnswerTrainingPromptAsync(DiscordMessageSnapshot snapshot, Action<string> logInfo)
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

        private async Task<bool> TryClickButtonAsync(DiscordMessageSnapshot snapshot, TrainingPromptResolution resolution)
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

                return await _chatClient.ClickMessageButtonAsync(snapshot.Id, button.RowIndex, button.ColumnIndex);
            }

            return false;
        }

        private static bool LabelsMatch(string left, string right)
        {
            return string.Equals(NormalizeLabel(left), NormalizeLabel(right), StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> WaitForTrainingConfirmationAsync(string afterMessageId, Action<string> logInfo)
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

            logInfo?.Invoke("Inicialize: training answer sent, but no 'Well done' confirmation was observed.");
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

        private sealed class InitializationStep
        {
            public InitializationStep(string canonical, string action, int defaultMs)
            {
                Canonical = canonical;
                Action = action;
                DefaultMs = defaultMs;
            }

            public string Canonical { get; }
            public string Action { get; }
            public int DefaultMs { get; }
        }
    }
}
