#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace EpicRPGBot.UI.Services
{
    internal sealed class DiscordWebViewLifecycle<TWebView> : IDisposable
        where TWebView : class
    {
        private readonly Func<TWebView> _createWebView;
        private readonly Func<TWebView, string, CancellationToken, Task> _initializeWebViewAsync;
        private readonly Func<TWebView, bool> _isWebViewReady;
        private readonly Func<TWebView, string> _readCurrentUrl;
        private readonly Action<TWebView> _releaseWebView;
        private readonly Func<string> _initialUrlProvider;
        private readonly DiscordWebViewDemandTracker _demandTracker =
            new DiscordWebViewDemandTracker();
        private readonly SemaphoreSlim _lifecycleGate = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _lifetimeCancellation = new CancellationTokenSource();
        private bool _disposed;

        public DiscordWebViewLifecycle(
            Func<TWebView> createWebView,
            Func<TWebView, string, CancellationToken, Task> initializeWebViewAsync,
            Func<TWebView, bool> isWebViewReady,
            Func<TWebView, string> readCurrentUrl,
            Action<TWebView> releaseWebView,
            Func<string> initialUrlProvider)
        {
            _createWebView = createWebView ?? throw new ArgumentNullException(nameof(createWebView));
            _initializeWebViewAsync = initializeWebViewAsync ??
                throw new ArgumentNullException(nameof(initializeWebViewAsync));
            _isWebViewReady = isWebViewReady ?? throw new ArgumentNullException(nameof(isWebViewReady));
            _readCurrentUrl = readCurrentUrl ?? throw new ArgumentNullException(nameof(readCurrentUrl));
            _releaseWebView = releaseWebView ?? throw new ArgumentNullException(nameof(releaseWebView));
            _initialUrlProvider = initialUrlProvider ?? throw new ArgumentNullException(nameof(initialUrlProvider));
        }

        public TWebView? Current { get; private set; }

        public bool HasDemand => _demandTracker.HasDemand;

        public bool IsSelected =>
            _demandTracker.GetCount(DiscordWebViewActivityReason.Selected) > 0;

        internal string LastUrl { get; private set; } = string.Empty;

        public async Task SetDemandAsync(
            DiscordWebViewActivityReason reason,
            bool isRequired,
            CancellationToken cancellationToken = default)
        {
            await _lifecycleGate.WaitAsync(cancellationToken);
            try
            {
                ThrowIfDisposed();
                _demandTracker.Set(reason, isRequired);
                if (HasDemand)
                {
                    await EnsureActiveAsync(cancellationToken);
                }
                else
                {
                    ReleaseCurrentWebView();
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public async Task AcquireAsync(
            DiscordWebViewActivityReason reason,
            CancellationToken cancellationToken = default)
        {
            await _lifecycleGate.WaitAsync(cancellationToken);
            try
            {
                ThrowIfDisposed();
                _demandTracker.Add(reason);
                try
                {
                    await EnsureActiveAsync(cancellationToken);
                }
                catch
                {
                    _demandTracker.Remove(reason);
                    if (!HasDemand)
                    {
                        ReleaseCurrentWebView();
                    }

                    throw;
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public async Task ReleaseAsync(DiscordWebViewActivityReason reason)
        {
            await _lifecycleGate.WaitAsync();
            try
            {
                _demandTracker.Remove(reason);
                if (!HasDemand)
                {
                    ReleaseCurrentWebView();
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetimeCancellation.Cancel();
            _demandTracker.Clear();
            if (_lifecycleGate.Wait(0))
            {
                try { ReleaseCurrentWebView(); }
                finally { _lifecycleGate.Release(); }
            }
            else
            {
                _ = DisposeAfterPendingOperationAsync();
            }
        }

        private async Task EnsureActiveAsync(CancellationToken cancellationToken)
        {
            var currentWebView = Current;
            if (currentWebView != null && _isWebViewReady(currentWebView))
            {
                return;
            }

            ReleaseCurrentWebView();
            var newWebView = _createWebView() ??
                throw new InvalidOperationException("The Discord WebView factory returned null.");
            Current = newWebView;
            try
            {
                using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _lifetimeCancellation.Token))
                {
                    await _initializeWebViewAsync(
                        newWebView,
                        ResolveInitialUrl(),
                        linkedCancellation.Token);
                }
            }
            catch
            {
                ReleaseCurrentWebView();
                throw;
            }
        }

        private string ResolveInitialUrl()
        {
            return string.IsNullOrWhiteSpace(LastUrl)
                ? _initialUrlProvider()?.Trim() ?? string.Empty
                : LastUrl;
        }

        private void ReleaseCurrentWebView()
        {
            var webView = Current;
            if (webView == null)
            {
                return;
            }

            RememberCurrentUrl(webView);
            Current = null;
            _releaseWebView(webView);
        }

        private void RememberCurrentUrl(TWebView webView)
        {
            try
            {
                var currentUrl = _readCurrentUrl(webView);
                if (Uri.TryCreate(currentUrl, UriKind.Absolute, out _))
                {
                    LastUrl = currentUrl;
                }
            }
            catch
            {
            }
        }

        private async Task DisposeAfterPendingOperationAsync()
        {
            await _lifecycleGate.WaitAsync();
            try { ReleaseCurrentWebView(); }
            finally { _lifecycleGate.Release(); }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DiscordWebViewLifecycle<TWebView>));
        }
    }
}
