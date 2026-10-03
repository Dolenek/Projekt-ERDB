using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.Tests.Services;

internal sealed class ScriptedCommandChatClient : IDiscordChatClient
{
    public const string MessagePrefix = "chat-messages-42-";
    public DiscordMessageSnapshot? Outgoing { get; set; } = new(MessagePrefix + "100", "rpg hunt", "Player");
    public DiscordMessageSnapshot? DirectReply { get; set; }
    public IReadOnlyList<DiscordMessageSnapshot> RecentMessages { get; set; } = Array.Empty<DiscordMessageSnapshot>();
    public Action? OnRecentMessagesRead { get; set; }
    public Action? OnReplyRead { get; set; }
    public int SendCount { get; private set; }
    public int ReplyReadCount { get; private set; }
    public bool IsReady => true;

    public Task EnsureInitializedAsync() => Task.CompletedTask;
    public void Reload() { }
    public Task NavigateToChannelAsync(string url) => Task.CompletedTask;
    public Task<string> GetLastMessageTextAsync() => Task.FromResult(string.Empty);
    public Task<DiscordMessageSnapshot> GetLatestMessageAsync() =>
        Task.FromResult(new DiscordMessageSnapshot(MessagePrefix + "99", "previous", "Player"));

    public Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount)
    {
        OnRecentMessagesRead?.Invoke();
        return Task.FromResult(RecentMessages);
    }

    public Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId)
    {
        ReplyReadCount++;
        OnReplyRead?.Invoke();
        return Task.FromResult(DirectReply!);
    }

    public Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(
        string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SendCount++;
        return Task.FromResult(Outgoing!);
    }

    public Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> OpenDirectMessageAsync(string conversationName, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> ClickMessageButtonAsync(string messageId, int rowIndex, int columnIndex,
        CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<string> GetPuzzleImageUrlForMessageIdAsync(string messageId) => Task.FromResult(string.Empty);
    public Task<byte[]> CaptureMessageImagePngAsync(string messageId) => Task.FromResult(Array.Empty<byte>());
}
