using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public interface IDiscordMessageNavigator
    {
        Task<bool> NavigateToMessageAsync(
            DiscordMessageReference reference,
            CancellationToken cancellationToken = default);
    }
}
