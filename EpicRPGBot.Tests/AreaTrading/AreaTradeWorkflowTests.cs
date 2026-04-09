using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.AreaTrading;
using EpicRPGBot.UI.Dismantling;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.AreaTrading
{
    public sealed class AreaTradeWorkflowTests
    {
        [Fact]
        public async Task DismantleFailure_DoesNotPreventTradeSteps()
        {
            var fileName = "area-trade-test-" + Guid.NewGuid().ToString("N") + ".ini";
            try
            {
                var chatClient = new FakeDiscordChatClient(new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["rpg p"] = QueueOf("Area: 5 (Max: 5)"),
                    ["rpg dismantle ultra log all"] = QueueOf("ultra log: 0 / 1"),
                    ["rpg dismantle hyper log all"] = QueueOf("hyper log: 0 / 1"),
                    ["rpg dismantle mega log all"] = QueueOf("mega log: 0 / 1"),
                    ["rpg dismantle super log all"] = QueueOf("8 epic log successfully crafted"),
                    ["rpg dismantle epic log all"] = QueueOf("20 wooden log successfully crafted"),
                    ["rpg dismantle epic fish all"] = QueueOf("epic fish: 0 / 1"),
                    ["rpg dismantle golden fish all"] = QueueOf("unexpected dismantle reply"),
                    ["rpg trade E all"] = QueueOf("you traded successfully"),
                    ["rpg trade A all"] = QueueOf("you traded successfully"),
                    ["rpg trade D all"] = QueueOf("you traded successfully")
                });

                var commandSender = new ConfirmedCommandSender(chatClient);
                var settingsService = CreateSettingsService(fileName, "5");
                var workflow = new AreaTradeWorkflow(
                    commandSender,
                    new DismantlingWorkflow(commandSender),
                    settingsService,
                    settingsService.LoadCurrent);
                var reports = new List<string>();

                var result = await workflow.RunAsync(reports.Add, CancellationToken.None);

                Assert.True(result.Completed);
                Assert.Contains("rpg trade E all", chatClient.SentCommands);
                Assert.Contains("rpg trade A all", chatClient.SentCommands);
                Assert.Contains("rpg trade D all", chatClient.SentCommands);
                Assert.Contains(reports, message => message.Contains("unrecognized reply for golden fish", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(reports, message => message.Contains("Continuing to the next area-trade step.", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                DeleteSettingsFile(fileName);
            }
        }

        [Fact]
        public async Task UnsupportedArea_SkipsInsteadOfFailing()
        {
            var fileName = "area-trade-test-" + Guid.NewGuid().ToString("N") + ".ini";
            try
            {
                var chatClient = new FakeDiscordChatClient(new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["rpg p"] = QueueOf("Area: 12 (Max: 12)")
                });

                var commandSender = new ConfirmedCommandSender(chatClient);
                var settingsService = CreateSettingsService(fileName, "12");
                var workflow = new AreaTradeWorkflow(
                    commandSender,
                    new DismantlingWorkflow(commandSender),
                    settingsService,
                    settingsService.LoadCurrent);
                var reports = new List<string>();

                var result = await workflow.RunAsync(reports.Add, CancellationToken.None);

                Assert.True(result.Completed);
                Assert.Contains("Area trade skipped", result.Summary, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain(chatClient.SentCommands, command => !string.Equals(command, "rpg p", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(reports, message => message.Contains("has no configured trade plan", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                DeleteSettingsFile(fileName);
            }
        }

        private static AppSettingsService CreateSettingsService(string fileName, string area)
        {
            var service = new AppSettingsService(new LocalSettingsStore(fileName));
            service.Save(AppSettingsSnapshot.Default.WithArea(area));
            return service;
        }

        private static Queue<string> QueueOf(params string[] replies)
        {
            return new Queue<string>(replies);
        }

        private static void DeleteSettingsFile(string fileName)
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EpicRPGBot.UI",
                "settings",
                fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private sealed class FakeDiscordChatClient : IDiscordChatClient
        {
            private readonly Dictionary<string, Queue<string>> _repliesByCommand;
            private readonly List<DiscordMessageSnapshot> _messages = new List<DiscordMessageSnapshot>();
            private readonly Dictionary<string, string> _commandsByOutgoingId = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly List<string> _sentCommands = new List<string>();
            private int _messageSequence;

            public FakeDiscordChatClient(Dictionary<string, Queue<string>> repliesByCommand)
            {
                _repliesByCommand = repliesByCommand;
            }

            public bool IsReady => true;
            public IReadOnlyList<string> SentCommands => _sentCommands;

            public Task EnsureInitializedAsync() => Task.CompletedTask;
            public void Reload() { }
            public Task NavigateToChannelAsync(string url) => Task.CompletedTask;
            public Task<string> GetLastMessageTextAsync() => Task.FromResult(_messages.LastOrDefault()?.Text);

            public Task<DiscordMessageSnapshot> GetLatestMessageAsync()
            {
                return Task.FromResult(_messages.LastOrDefault() ?? new DiscordMessageSnapshot(string.Empty, string.Empty, string.Empty));
            }

            public Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount)
            {
                return Task.FromResult<IReadOnlyList<DiscordMessageSnapshot>>(_messages.TakeLast(maxCount).ToArray());
            }

            public Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId)
            {
                if (!_commandsByOutgoingId.TryGetValue(outgoingMessageId ?? string.Empty, out var command) ||
                    !_repliesByCommand.TryGetValue(command, out var replies) ||
                    replies.Count == 0)
                {
                    return Task.FromResult<DiscordMessageSnapshot>(null);
                }

                var reply = CreateSnapshot("EPIC RPG", replies.Dequeue());
                _messages.Add(reply);
                return Task.FromResult(reply);
            }

            public Task<bool> OpenDirectMessageAsync(string conversationName, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(string message, CancellationToken cancellationToken = default)
            {
                var outgoing = CreateSnapshot("TestPlayer", message);
                _messages.Add(outgoing);
                _commandsByOutgoingId[outgoing.Id] = message;
                _sentCommands.Add(message);
                return Task.FromResult(outgoing);
            }

            public Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<bool> ClickMessageButtonAsync(string messageId, int rowIndex, int columnIndex, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId) => Task.FromResult(string.Empty);
            public Task<byte[]> CaptureMessageImagePngAsync(string messageId) => Task.FromResult(Array.Empty<byte>());

            private DiscordMessageSnapshot CreateSnapshot(string author, string text)
            {
                _messageSequence++;
                return new DiscordMessageSnapshot("m" + _messageSequence, text, author, text);
            }
        }
    }
}
