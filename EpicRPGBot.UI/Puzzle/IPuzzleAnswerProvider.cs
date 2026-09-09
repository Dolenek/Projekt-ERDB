using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Puzzle
{
    public interface IPuzzleAnswerProvider
    {
        string DescribeConfiguration();

        Task<PuzzleAnswerResult> SolveAsync(byte[] imageBytes, CancellationToken cancellationToken);
    }
}
