using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.Tests.Pets;

internal sealed class FakePetChatClient : IDiscordChatClient
{
    public DiscordMessageSnapshot Current { get; set; } = new("1", "", "EPIC RPG");
    public DiscordMessageSnapshot? NextPage { get; set; }
    public int Clicks { get; private set; }
    public bool IsReady => true;
    public Task EnsureInitializedAsync() => Task.CompletedTask;
    public void Reload() { }
    public Task NavigateToChannelAsync(string url) => Task.CompletedTask;
    public Task<string> GetLastMessageTextAsync() => Task.FromResult(Current.RenderedText);
    public Task<DiscordMessageSnapshot> GetLatestMessageAsync() => Task.FromResult(Current);
    public Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount) =>
        Task.FromResult<IReadOnlyList<DiscordMessageSnapshot>>(new[] { Current });
    public Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId) => Task.FromResult(Current);
    public Task<bool> OpenDirectMessageAsync(string conversationName, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(string message, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DiscordMessageSnapshot("0", message, "friendr"));
    public Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> ClickMessageButtonAsync(string messageId, int rowIndex, int columnIndex, CancellationToken cancellationToken = default)
    {
        Clicks++;
        if (NextPage != null) Current = NextPage;
        return Task.FromResult(NextPage != null);
    }
    public Task<string> GetPuzzleImageUrlForMessageIdAsync(string messageId) => Task.FromResult("");
    public Task<byte[]> CaptureMessageImagePngAsync(string messageId) => Task.FromResult(Array.Empty<byte>());
}
