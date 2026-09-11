using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public interface IDiscordAttachmentImageClient
    {
        Task<string> GetMessageImageUrlForMessageIdAsync(string messageId);
    }
}
