using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Services
{
    public sealed class ConfirmedCommandSenderRetryTests
    {
        [Fact]
        public async Task SendAsync_DoesNotBlindlyResendDungeonEntry()
        {
            var chatClient = new NonRegisteringChatClient();
            var sender = new ConfirmedCommandSender(chatClient);

            var result = await sender.SendAsync("rpg dung <@123456>");

            Assert.False(result.IsConfirmed);
            Assert.Equal(1, result.AttemptCount);
            Assert.Equal(1, chatClient.SendCount);
        }

        private sealed class NonRegisteringChatClient : IDiscordChatClient
        {
            public int SendCount { get; private set; }

            public bool IsReady => true;

            public Task EnsureInitializedAsync() => Task.CompletedTask;

            public void Reload()
            {
            }

            public Task NavigateToChannelAsync(string url) => Task.CompletedTask;

            public Task<string> GetLastMessageTextAsync() => Task.FromResult(string.Empty);

            public Task<DiscordMessageSnapshot> GetLatestMessageAsync()
            {
                return Task.FromResult(new DiscordMessageSnapshot(string.Empty, string.Empty));
            }

            public Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount)
            {
                return Task.FromResult<IReadOnlyList<DiscordMessageSnapshot>>(Array.Empty<DiscordMessageSnapshot>());
            }

            public Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId)
            {
                return Task.FromResult<DiscordMessageSnapshot>(null);
            }

            public Task<bool> OpenDirectMessageAsync(string conversationName, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(
                string message,
                CancellationToken cancellationToken = default)
            {
                SendCount++;
                return Task.FromResult<DiscordMessageSnapshot>(null);
            }

            public Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<bool> ClickMessageButtonAsync(
                string messageId,
                int rowIndex,
                int columnIndex,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(false);
            }

            public Task<string> GetPuzzleImageUrlForMessageIdAsync(string messageId)
            {
                return Task.FromResult(string.Empty);
            }

            public Task<byte[]> CaptureMessageImagePngAsync(string messageId)
            {
                return Task.FromResult(Array.Empty<byte>());
            }
        }
    }
}
