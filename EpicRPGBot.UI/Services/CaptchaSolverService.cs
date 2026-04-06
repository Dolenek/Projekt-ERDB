using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Captcha;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaSolverService
    {
        private readonly IDiscordChatClient _chatClient;
        private readonly CaptchaDebugArtifactWriter _debugWriter = new CaptchaDebugArtifactWriter();
        private bool _captchaInProgress;
        private CancellationTokenSource _captchaAttemptCancellation;
        private HttpClient _httpClient;
        private ICaptchaAnswerProvider _answerProvider;

        public CaptchaSolverService(IDiscordChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

        public bool IsBusy => _captchaInProgress;

        public async Task TrySolveAsync(
            string targetMessageId,
            string lastMessageId,
            string previousMessageId,
            Func<string, Task<bool>> sendAndEmitAsync,
            Action pauseTimers,
            Action resumeTimers,
            Action<string> reportInfo)
        {
            if (_captchaInProgress)
            {
                reportInfo?.Invoke("Solve already in progress; duplicate guard trigger ignored.");
                return;
            }

            _captchaInProgress = true;
            using (var cancellation = new CancellationTokenSource())
            try
            {
                _captchaAttemptCancellation = cancellation;
                var cancellationToken = cancellation.Token;
                reportInfo?.Invoke($"Starting guard solve for message {targetMessageId}.");
                pauseTimers?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();

                var provider = EnsureAnswerProvider(reportInfo);
                if (provider == null)
                {
                    reportInfo?.Invoke("Solver unavailable (provider init failed).");
                    return;
                }

                if (string.IsNullOrWhiteSpace(targetMessageId))
                {
                    reportInfo?.Invoke("Cannot solve: message id is empty.");
                    return;
                }

                var adjacentMessageId = targetMessageId == lastMessageId ? previousMessageId : lastMessageId;
                var capture = await TryLoadCaptchaImageAsync(targetMessageId, "selected", reportInfo, cancellationToken);
                if (capture == null && !string.IsNullOrWhiteSpace(adjacentMessageId))
                {
                    reportInfo?.Invoke("Primary message had no image; trying adjacent message.");
                    capture = await TryLoadCaptchaImageAsync(adjacentMessageId, "adjacent", reportInfo, cancellationToken);
                }

                if (capture == null || capture.Bytes == null || capture.Bytes.Length == 0)
                {
                    reportInfo?.Invoke("Captcha image not found in selected/adjacent messages.");
                    return;
                }

                var debugPath = _debugWriter.TryWriteCapture(capture.MessageId, capture.Source, capture.Url, capture.Bytes);
                if (!string.IsNullOrWhiteSpace(debugPath))
                {
                    reportInfo?.Invoke($"Captcha debug artifact saved: {debugPath}");
                }

                reportInfo?.Invoke($"Captcha image via URL: {capture.Url}");

                var start = Stopwatch.GetTimestamp();
                reportInfo?.Invoke("Submitting captcha image to the vision solver.");
                var result = await provider.SolveAsync(capture.Bytes, cancellationToken);

                var elapsedMs = (int)(1000.0 * (Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency);

                if (!result.IsMatch || string.IsNullOrWhiteSpace(result.Label))
                {
                    reportInfo?.Invoke($"Solver uncertain via {result.Method} ({result.Detail}, {elapsedMs} ms). Skipping.");
                    return;
                }

                reportInfo?.Invoke($"Solver answer '{result.Label}' via {result.Method} ({result.Detail}, {elapsedMs} ms). Sending.");
                cancellationToken.ThrowIfCancellationRequested();
                var sent = await sendAndEmitAsync(result.Label);
                reportInfo?.Invoke(sent
                    ? $"Captcha answer '{result.Label}' sent to chat."
                    : $"Captcha answer '{result.Label}' could not be sent to chat.");
            }
            catch (OperationCanceledException)
            {
                reportInfo?.Invoke("Captcha solve cancelled after guard cleared.");
            }
            catch (Exception ex)
            {
                reportInfo?.Invoke($"SolveCaptcha error: {ex.Message}");
            }
            finally
            {
                _captchaAttemptCancellation = null;
                resumeTimers?.Invoke();
                _captchaInProgress = false;
            }
        }

        public void CancelCurrentSolve()
        {
            try
            {
                _captchaAttemptCancellation?.Cancel();
            }
            catch
            {
            }
        }

        private ICaptchaAnswerProvider EnsureAnswerProvider(Action<string> reportInfo)
        {
            if (_answerProvider != null)
            {
                return _answerProvider;
            }

            try
            {
                var settings = CaptchaSettings.LoadDefault();
                _answerProvider = new CaptchaProviderFactory().Create(settings);
                reportInfo?.Invoke("Solver initialized (" + _answerProvider.DescribeConfiguration() + ").");
            }
            catch (Exception ex)
            {
                reportInfo?.Invoke($"Solver init failed: {ex.Message}");
                _answerProvider = null;
            }

            return _answerProvider;
        }

        private HttpClient EnsureHttpClient()
        {
            if (_httpClient != null)
            {
                return _httpClient;
            }

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(4)
            };

            return _httpClient;
        }

        private async Task<CaptchaImageLoadResult> TryLoadCaptchaImageAsync(
            string messageId,
            string label,
            Action<string> reportInfo,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(messageId))
            {
                return null;
            }

            var url = await _chatClient.GetCaptchaImageUrlForMessageIdAsync(messageId);
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(url))
            {
                try
                {
                    var bytes = await EnsureHttpClient().GetByteArrayAsync(url);
                    cancellationToken.ThrowIfCancellationRequested();
                    reportInfo?.Invoke($"Resolved {label} captcha image URL.");
                    return new CaptchaImageLoadResult(messageId, "message-url", url, bytes);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    reportInfo?.Invoke($"Captcha image URL download failed for {label} message: {ex.Message}");
                }
            }

            return null;
        }

        private sealed class CaptchaImageLoadResult
        {
            public CaptchaImageLoadResult(string messageId, string source, string url, byte[] bytes)
            {
                MessageId = messageId ?? string.Empty;
                Source = source ?? string.Empty;
                Url = url ?? string.Empty;
                Bytes = bytes ?? Array.Empty<byte>();
            }

            public string MessageId { get; }

            public string Source { get; }

            public string Url { get; }

            public byte[] Bytes { get; }
        }
    }
}
