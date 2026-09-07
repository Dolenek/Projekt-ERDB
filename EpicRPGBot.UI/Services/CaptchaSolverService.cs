using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Captcha;

namespace EpicRPGBot.UI.Services
{
    public sealed class CaptchaSolverService : IDisposable
    {
        private readonly ICaptchaImageSource _images;
        private readonly Lazy<ICaptchaAnswerProvider> _provider;
        private readonly Func<bool> _automaticAnswersEnabled;
        private CancellationTokenSource _attempt;
        private int _busy;
        private int _disposed;
        private int _providerDisposed;

        public CaptchaSolverService(IDiscordChatClient chatClient)
            : this(new CaptchaImageSource(chatClient ?? throw new ArgumentNullException(nameof(chatClient))),
                () => new CaptchaProviderFactory().Create(CaptchaSettings.LoadDefault()),
                () => CaptchaSettings.LoadDefault().AutomaticAnswersEnabled) { }

        public CaptchaSolverService(ICaptchaImageSource images, Func<ICaptchaAnswerProvider> provider,
            Func<bool> automaticAnswersEnabled)
        {
            _images = images ?? throw new ArgumentNullException(nameof(images));
            _provider = new Lazy<ICaptchaAnswerProvider>(provider);
            _automaticAnswersEnabled = automaticAnswersEnabled;
        }

        public bool IsBusy => Volatile.Read(ref _busy) != 0;

        public async Task TrySolveAsync(string targetId, string lastId, string previousId,
            Func<string, CancellationToken, Task<bool>> sendAnswer, Action pauseTimers,
            Func<bool> incidentIsCurrent, Action<string> report)
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(CaptchaSolverService));
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;
            using (var cancellation = new CancellationTokenSource())
            {
                _attempt = cancellation;
                try
                {
                    if (string.IsNullOrWhiteSpace(targetId) || !incidentIsCurrent()) return;
                    pauseTimers?.Invoke();
                    var adjacentId = targetId == lastId ? previousId : lastId;
                    var bytes = await _images.LoadAsync(targetId, adjacentId, report, cancellation.Token);
                    if (bytes == null) { report?.Invoke("Captcha image unavailable. Waiting for manual resolution."); return; }
                    await RecognizeAndSendAsync(bytes, sendAnswer, incidentIsCurrent, report, cancellation.Token);
                }
                catch (OperationCanceledException) { report?.Invoke("Captcha attempt cancelled."); }
                catch (Exception ex) { report?.Invoke("Captcha solver failed; waiting for manual resolution: " + ex.Message); }
                finally
                {
                    _attempt = null;
                    Interlocked.Exchange(ref _busy, 0);
                    if (Volatile.Read(ref _disposed) != 0) DisposeProvider();
                }
            }
        }

        private async Task RecognizeAndSendAsync(byte[] bytes, Func<string, CancellationToken, Task<bool>> sendAnswer,
            Func<bool> incidentIsCurrent, Action<string> report, CancellationToken token)
        {
            var provider = await Task.Run(() => _provider.Value, token);
            token.ThrowIfCancellationRequested();
            var watch = Stopwatch.StartNew();
            var result = await provider.SolveAsync(bytes, token);
            report?.Invoke("Local result: " + result.Detail + " (" + watch.ElapsedMilliseconds + " ms).");
            if (!result.IsMatch || !result.AutomaticSubmissionAllowed || !_automaticAnswersEnabled())
            {
                report?.Invoke("No automatic answer: uncertain, unvalidated, or observation mode. Waiting for manual resolution.");
                return;
            }
            token.ThrowIfCancellationRequested();
            if (!incidentIsCurrent()) return;
            var sent = await sendAnswer(result.Label, token);
            report?.Invoke(sent ? "Captcha answer '" + result.Label + "' sent. Waiting for EPIC GUARD confirmation."
                : "Captcha answer not sent. Waiting for manual resolution.");
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            CancelCurrentSolve();
            if (!IsBusy) DisposeProvider();
        }

        private void DisposeProvider()
        {
            if (_provider.IsValueCreated && Interlocked.Exchange(ref _providerDisposed, 1) == 0)
                (_provider.Value as IDisposable)?.Dispose();
        }

        public void CancelCurrentSolve()
        {
            try { _attempt?.Cancel(); }
            catch (ObjectDisposedException) { }
        }
    }
}
