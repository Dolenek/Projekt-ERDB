using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed class DiscordAttachmentImageSource
    {
        private const int MaximumImageBytes = 8 * 1024 * 1024;
        private static readonly HttpClient Client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly IDiscordChatClient _chatClient;

        public DiscordAttachmentImageSource(IDiscordChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public async Task<byte[]> LoadAsync(string messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(messageId)) return null;
            var url = _chatClient is IDiscordAttachmentImageClient attachmentClient
                ? await attachmentClient.GetMessageImageUrlForMessageIdAsync(messageId)
                : await _chatClient.GetPuzzleImageUrlForMessageIdAsync(messageId);
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(url)) return null;

            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
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
            if (response.Content.Headers.ContentLength > MaximumImageBytes)
                throw new InvalidOperationException("Discord attachment is too large.");
            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var output = new MemoryStream())
            {
                var buffer = new byte[8192];
                int count;
                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    if (output.Length + count > MaximumImageBytes)
                        throw new InvalidOperationException("Discord attachment is too large.");
                    output.Write(buffer, 0, count);
                }

                return output.ToArray();
            }
        }
    }
}
