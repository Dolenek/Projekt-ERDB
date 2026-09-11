using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public interface IDuelDiscordClient : IDiscordChatClient
    {
        Task<IReadOnlyList<DiscordChannelReference>> DiscoverCategoryChannelsAsync(
            string categoryId,
            CancellationToken cancellationToken = default);

        Task<bool> NavigateToChannelAndWaitAsync(
            string url,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<DiscordMessageSnapshot>> GetMessagesSinceAsync(
            DateTimeOffset cutoffUtc,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyDictionary<string, int>> GetChannelMentionCountsAsync(
            IReadOnlyList<string> channelIds,
            CancellationToken cancellationToken = default);

        Task<bool> ClickMessageButtonByLabelAsync(
            string messageId,
            string label,
            CancellationToken cancellationToken = default);

        Task<bool> AddReactionAsync(
            string messageId,
            string emojiName,
            CancellationToken cancellationToken = default);

        Task<bool> RemoveOwnReactionAsync(
            string messageId,
            string emojiName,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteOwnMessageAsync(
            string messageId,
            CancellationToken cancellationToken = default);
    }
}
