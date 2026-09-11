using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.Tests.Duel;

internal sealed class FakeDuelDiscordClient : IDuelDiscordClient
{
    private int _sentMessageNumber;

    public bool IsReady => true;
    public string CurrentUrl { get; private set; } = string.Empty;
    public IList<DiscordChannelReference> DiscoveredChannels { get; } = new List<DiscordChannelReference>();
    public IDictionary<string, List<DiscordMessageSnapshot>> MessagesByUrl { get; } = new Dictionary<string, List<DiscordMessageSnapshot>>(StringComparer.Ordinal);
    public Queue<IReadOnlyList<DiscordMessageSnapshot>> FollowupsAfterSend { get; } = new();
    public IList<string> SentMessages { get; } = new List<string>();
    public IList<string> NavigatedUrls { get; } = new List<string>();
    public IDictionary<string, int> MentionCounts { get; } = new Dictionary<string, int>();
    public Queue<IReadOnlyDictionary<string, int>> MentionCountSnapshots { get; } = new();
    public IList<(string MessageId, string Emoji)> AddedReactions { get; } = new List<(string, string)>();
    public IList<(string MessageId, string Emoji)> RemovedReactions { get; } = new List<(string, string)>();
    public IList<(string MessageId, string Label)> ClickedButtons { get; } = new List<(string, string)>();
    public IList<string> DeletedMessages { get; } = new List<string>();
    public ISet<string> FailedReactionMessages { get; } = new HashSet<string>(StringComparer.Ordinal);
    public ISet<string> UncertainOutgoingCommands { get; } = new HashSet<string>(StringComparer.Ordinal);
    public ISet<string> FailedDeleteMessages { get; } = new HashSet<string>(StringComparer.Ordinal);
    public Action<string>? MessageSent { get; set; }

    public Task EnsureInitializedAsync() => Task.CompletedTask;
    public void Reload() { }
    public Task NavigateToChannelAsync(string url)
    {
        CurrentUrl = url;
        NavigatedUrls.Add(url);
        EnsureCurrentMessages();
        return Task.CompletedTask;
    }
    public async Task<bool> NavigateToChannelAndWaitAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await NavigateToChannelAsync(url);
        return true;
    }
    public Task<IReadOnlyList<DiscordChannelReference>> DiscoverCategoryChannelsAsync(
        string categoryId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((IReadOnlyList<DiscordChannelReference>)DiscoveredChannels.ToArray());
    public Task<string> GetLastMessageTextAsync() =>
        Task.FromResult(CurrentMessages().LastOrDefault()?.Text ?? string.Empty);
    public Task<DiscordMessageSnapshot> GetLatestMessageAsync() =>
        Task.FromResult(CurrentMessages().LastOrDefault()!);
    public Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount)
    {
        var messages = CurrentMessages().TakeLast(maxCount).ToArray();
        return Task.FromResult((IReadOnlyList<DiscordMessageSnapshot>)messages);
    }
    public Task<IReadOnlyList<DiscordMessageSnapshot>> GetMessagesSinceAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default)
    {
        var messages = CurrentMessages()
            .Where(message => !message.CreatedAtUtc.HasValue || message.CreatedAtUtc >= cutoffUtc)
            .ToArray();
        return Task.FromResult((IReadOnlyList<DiscordMessageSnapshot>)messages);
    }
    public Task<IReadOnlyDictionary<string, int>> GetChannelMentionCountsAsync(
        IReadOnlyList<string> channelIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = MentionCountSnapshots.Count > 0 ? MentionCountSnapshots.Dequeue() :
            new Dictionary<string, int>(MentionCounts);
        return Task.FromResult(snapshot);
    }
    public Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId)
    {
        var messages = CurrentMessages();
        var index = messages.FindIndex(message => message.Id == outgoingMessageId);
        return Task.FromResult(index >= 0 ? messages.Skip(index + 1).FirstOrDefault()! : null!);
    }
    public Task<bool> OpenDirectMessageAsync(
        string conversationName,
        CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SentMessages.Add(message);
        if (UncertainOutgoingCommands.Contains(message))
        {
            MessageSent?.Invoke(message);
            return Task.FromResult<DiscordMessageSnapshot>(null!);
        }

        var snapshot = DuelTestMessages.Message(
            "sent-" + ++_sentMessageNumber,
            message,
            DateTimeOffset.UtcNow,
            "self",
            "100");
        CurrentMessages().Add(snapshot);
        MessageSent?.Invoke(message);
        if (FollowupsAfterSend.Count > 0)
        {
            CurrentMessages().AddRange(FollowupsAfterSend.Dequeue());
        }

        return Task.FromResult<DiscordMessageSnapshot>(snapshot);
    }

    public async Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default) =>
        await SendMessageAndWaitForOutgoingAsync(message, cancellationToken) != null;

    public Task<bool> ClickMessageButtonAsync(
        string messageId,
        int rowIndex,
        int columnIndex,
        CancellationToken cancellationToken = default)
    {
        var message = CurrentMessages().FirstOrDefault(candidate => candidate.Id == messageId);
        var label = message?.Buttons.FirstOrDefault(
            button => button.RowIndex == rowIndex && button.ColumnIndex == columnIndex)?.Label;
        if (label == null)
        {
            return Task.FromResult(false);
        }

        ClickedButtons.Add((messageId, label));
        return Task.FromResult(true);
    }

    public Task<bool> ClickMessageButtonByLabelAsync(
        string messageId,
        string label,
        CancellationToken cancellationToken = default)
    {
        var message = CurrentMessages().FirstOrDefault(candidate => candidate.Id == messageId);
        var found = message?.Buttons.FirstOrDefault(button =>
            string.Equals(button.Label, label, StringComparison.OrdinalIgnoreCase));
        return found == null
            ? Task.FromResult(false)
            : ClickMessageButtonAsync(messageId, found.RowIndex, found.ColumnIndex, cancellationToken);
    }

    public Task<bool> AddReactionAsync(
        string messageId,
        string emojiName,
        CancellationToken cancellationToken = default)
    {
        if (FailedReactionMessages.Contains(messageId))
        {
            return Task.FromResult(false);
        }

        AddedReactions.Add((messageId, emojiName));
        return Task.FromResult(true);
    }

    public Task<bool> RemoveOwnReactionAsync(
        string messageId,
        string emojiName,
        CancellationToken cancellationToken = default)
    {
        RemovedReactions.Add((messageId, emojiName));
        return Task.FromResult(true);
    }

    public Task<bool> DeleteOwnMessageAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        var succeeded = !FailedDeleteMessages.Contains(messageId);
        if (succeeded) DeletedMessages.Add(messageId);
        return Task.FromResult(succeeded);
    }

    public Task<string> GetPuzzleImageUrlForMessageIdAsync(string messageId) =>
        Task.FromResult(string.Empty);

    public Task<byte[]> CaptureMessageImagePngAsync(string messageId) =>
        Task.FromResult(Array.Empty<byte>());

    private List<DiscordMessageSnapshot> CurrentMessages()
    {
        EnsureCurrentMessages();
        return MessagesByUrl[CurrentUrl];
    }

    private void EnsureCurrentMessages()
    {
        if (!MessagesByUrl.ContainsKey(CurrentUrl))
        {
            MessagesByUrl[CurrentUrl] = new List<DiscordMessageSnapshot>();
        }
    }
}
