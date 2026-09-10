using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public interface IPuzzleAnswerChatClient
    {
        Task<bool> SendPuzzleAnswerOnceAsync(string answer, Func<bool> incidentIsCurrent, CancellationToken cancellationToken);
    }
}
