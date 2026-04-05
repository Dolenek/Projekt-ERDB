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

                var bytes = await _chatClient.CaptureMessageImagePngAsync(targetMessageId);
                cancellationToken.ThrowIfCancellationRequested();
                var url = string.Empty;

                if (bytes == null || bytes.Length == 0)
                {
                    url = await _chatClient.GetCaptchaImageUrlForMessageIdAsync(targetMessageId);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if ((bytes == null || bytes.Length == 0) && string.IsNullOrWhiteSpace(url))
                {
                    var adjacentMessageId = targetMessageId == lastMessageId ? previousMessageId : lastMessageId;
                    if (!string.IsNullOrWhiteSpace(adjacentMessageId))
                    {
                        reportInfo?.Invoke("Primary message had no image; trying adjacent message.");
                        bytes = await _chatClient.CaptureMessageImagePngAsync(adjacentMessageId);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (bytes == null || bytes.Length == 0)
                        {
                            url = await _chatClient.GetCaptchaImageUrlForMessageIdAsync(adjacentMessageId);
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }

                if ((bytes == null || bytes.Length == 0) && string.IsNullOrWhiteSpace(url))
                {
                    reportInfo?.Invoke("Captcha image not found in selected/adjacent messages.");
                    return;
                }

                if (bytes == null || bytes.Length == 0)
                {
                    reportInfo?.Invoke($"Captcha image via URL: {url}");
                    try
                    {
                        bytes = await EnsureHttpClient().GetByteArrayAsync(url);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    catch (Exception ex)
                    {
                        reportInfo?.Invoke($"Download failed: {ex.Message}");
                        return;
                    }
                }
                else
                {
                    reportInfo?.Invoke("Captcha image captured via DevTools screenshot.");
                }

                var start = Stopwatch.GetTimestamp();
                reportInfo?.Invoke("Submitting captcha image to the vision solver.");
                var result = await provider.SolveAsync(bytes, cancellationToken);
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
    }
}
