using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public interface ICaptchaImageSource
    {
        Task<byte[]> LoadAsync(string targetId, string adjacentId,
            Action<string> report, CancellationToken cancellationToken);
    }

    public sealed class CaptchaImageSource : ICaptchaImageSource
    {
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly IDiscordChatClient _chat;
        private readonly CaptchaDebugArtifactWriter _debug = new CaptchaDebugArtifactWriter();
        public CaptchaImageSource(IDiscordChatClient chat) { _chat = chat; }

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
            var url = await _chat.GetCaptchaImageUrlForMessageIdAsync(messageId);
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                var bytes = await DownloadAsync(url, token);
                var debugPath = _debug.TryWriteCapture(messageId, "message-url", url, bytes);
                if (!string.IsNullOrEmpty(debugPath)) report?.Invoke("Captcha debug artifact: " + debugPath);
                return bytes;
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                report?.Invoke("Captcha attachment download timed out.");
                return null;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                report?.Invoke("Captcha attachment download failed: " + ex.Message);
                return null;
            }
        }

        private static async Task<byte[]> DownloadAsync(string url, CancellationToken token)
        {
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(10));
                using (var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token))
                {
                    response.EnsureSuccessStatusCode();
                    return await ReadBoundedAsync(response, deadline.Token);
                }
            }
        }

        private static async Task<byte[]> ReadBoundedAsync(HttpResponseMessage response, CancellationToken token)
        {
            const int limit = 8 * 1024 * 1024;
            if (response.Content.Headers.ContentLength > limit) throw new InvalidOperationException("Captcha attachment is too large.");
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var output = new System.IO.MemoryStream())
            {
                var buffer = new byte[8192];
                int count;
                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    if (output.Length + count > limit) throw new InvalidOperationException("Captcha attachment is too large.");
                    output.Write(buffer, 0, count);
                }
                return output.ToArray();
            }
        }
    }
}
