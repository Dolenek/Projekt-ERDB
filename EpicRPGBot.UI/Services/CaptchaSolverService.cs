using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaSolverService
    {
        private readonly IDiscordChatClient _chatClient;
        private bool _captchaInProgress;
        private CancellationTokenSource _captchaAttemptCancellation;
        private HttpClient _httpClient;
        private CaptchaClassifier _classifier;

        public CaptchaSolverService(IDiscordChatClient chatClient)
        {
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
        }

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
                return;
            }

            _captchaInProgress = true;
            using (var cancellation = new CancellationTokenSource())
            try
            {
                _captchaAttemptCancellation = cancellation;
                var cancellationToken = cancellation.Token;
                pauseTimers?.Invoke();
                cancellationToken.ThrowIfCancellationRequested();

                var classifier = EnsureClassifier(reportInfo);
                if (classifier == null)
                {
                    reportInfo?.Invoke("Solver unavailable (classifier init failed).");
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
                var result = classifier.Classify(bytes);
                var elapsedMs = (int)(1000.0 * (Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency);

                if (!result.IsMatch || string.IsNullOrWhiteSpace(result.Label))
                {
                    reportInfo?.Invoke($"Classifier uncertain; closest='{(string.IsNullOrWhiteSpace(result.Label) ? "<none>" : result.Label)}' (dist={result.Distance}, method={result.Method}, {elapsedMs} ms). Skipping.");
                    return;
                }

                reportInfo?.Invoke($"Classifier answer '{result.Label}' (dist={result.Distance}, method={result.Method}, {elapsedMs} ms). Sending.");
                cancellationToken.ThrowIfCancellationRequested();
                await sendAndEmitAsync(result.Label);
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

        private CaptchaClassifier EnsureClassifier(Action<string> reportInfo)
        {
            if (_classifier != null)
            {
                return _classifier;
            }

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var defaultRefs = Path.Combine(baseDir, "CaptchaRefs");
            var refsDir = Env.Get("CAPTCHA_REFS_DIR", defaultRefs);
            var threshold = 12;

            try
            {
                var raw = Env.Get("CAPTCHA_HASH_THRESHOLD", null);
                if (!string.IsNullOrWhiteSpace(raw) && int.TryParse(raw, out var parsed) && parsed > 0 && parsed <= 64)
                {
                    threshold = parsed;
                }
            }
            catch
            {
            }

            try
            {
                _classifier = new CaptchaClassifier(refsDir, threshold);
                reportInfo?.Invoke($"Solver initialized (refs={refsDir}, threshold={threshold}).");
            }
            catch (Exception ex)
            {
                reportInfo?.Invoke($"Solver init failed: {ex.Message}");
                _classifier = null;
            }

            return _classifier;
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
