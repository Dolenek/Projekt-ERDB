using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Captcha
{
    public interface ICaptchaAnswerProvider
    {
        string DescribeConfiguration();

        Task<CaptchaAnswerResult> SolveAsync(byte[] imageBytes, CancellationToken cancellationToken);
    }
}
