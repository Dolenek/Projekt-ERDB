using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public interface IDiscordChatClient
    {
        bool IsReady { get; }

        Task EnsureInitializedAsync();
        void Reload();
        Task NavigateToChannelAsync(string url);
        Task<string> GetLastMessageTextAsync();
        Task<DiscordMessageSnapshot> GetLatestMessageAsync();
        Task<IReadOnlyList<DiscordMessageSnapshot>> GetRecentMessagesAsync(int maxCount);
        Task<DiscordMessageSnapshot> GetEpicReplyAfterMessageAsync(string outgoingMessageId);
        Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(string message, CancellationToken cancellationToken = default);
        Task<bool> SendMessageAsync(string message, CancellationToken cancellationToken = default);
        Task<bool> ClickMessageButtonAsync(string messageId, int rowIndex, int columnIndex, CancellationToken cancellationToken = default);
        Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId);
        Task<byte[]> CaptureMessageImagePngAsync(string messageId);
    }
}
