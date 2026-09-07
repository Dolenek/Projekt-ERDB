using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public interface ICaptchaAnswerChatClient
    {
        Task<bool> SendCaptchaAnswerOnceAsync(string answer, Func<bool> incidentIsCurrent, CancellationToken cancellationToken);
    }
}
