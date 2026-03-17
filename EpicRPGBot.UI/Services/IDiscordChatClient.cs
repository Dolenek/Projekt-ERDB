using System.Collections.Generic;
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
        Task<bool> HasEpicReplyForCommandAfterMessageAsync(string anchorMessageId, string command);
        Task<DiscordMessageSnapshot> SendMessageAndWaitForOutgoingAsync(string message);
        Task<bool> SendMessageAsync(string message);
        Task<string> GetCaptchaImageUrlForMessageIdAsync(string messageId);
        Task<byte[]> CaptureMessageImagePngAsync(string messageId);
    }
}
