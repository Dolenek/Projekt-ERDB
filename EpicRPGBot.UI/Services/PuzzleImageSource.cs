using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public interface IPuzzleImageSource
    {
        Task<byte[]> LoadAsync(string targetId, string adjacentId,
            Action<string> report, CancellationToken cancellationToken);
    }

    public sealed class PuzzleImageSource : IPuzzleImageSource
    {
        private readonly DiscordAttachmentImageSource _attachmentSource;
        private readonly PuzzleDebugArtifactWriter _debug = new PuzzleDebugArtifactWriter();
        public PuzzleImageSource(IDiscordChatClient chat)
        {
            _attachmentSource = new DiscordAttachmentImageSource(chat);
        }

        public async Task<byte[]> LoadAsync(string targetId, string adjacentId,
            Action<string> report, CancellationToken cancellationToken)
        {
            var bytes = await TryLoadAsync(targetId, report, cancellationToken);
            if (bytes != null || string.IsNullOrWhiteSpace(adjacentId) || adjacentId == targetId) return bytes;
            return await TryLoadAsync(adjacentId, report, cancellationToken);
        }

        private async Task<byte[]> TryLoadAsync(string messageId, Action<string> report, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return null;
            try
            {
                var bytes = await _attachmentSource.LoadAsync(messageId, token);
                if (bytes == null) return null;
                var debugPath = _debug.TryWriteCapture(messageId, "message-url", string.Empty, bytes);
                if (!string.IsNullOrEmpty(debugPath)) report?.Invoke("Puzzle debug artifact: " + debugPath);
                return bytes;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                report?.Invoke("Puzzle attachment download timed out.");
                return null;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                report?.Invoke("Puzzle attachment download failed: " + ex.Message);
                return null;
            }
        }

    }
}
