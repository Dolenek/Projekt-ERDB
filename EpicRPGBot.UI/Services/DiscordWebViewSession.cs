using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordWebViewSession : IDisposable
    {
        private const int InitialNavigationTimeoutMs = 30000;
        private readonly DiscordChatClient _chatClient;
        private readonly ContentControl _host;
        private readonly Panel _backgroundParking;
        private readonly DiscordWebViewReference _webViewReference;
        private readonly DiscordWebViewLifecycle<WebView2> _lifecycle;
        private bool _disposed;

        private DiscordWebViewSession(
            ContentControl host,
            Panel backgroundParking,
            Func<string> initialUrlProvider,
            IDiscordWebViewFactory webViewFactory,
            DiscordWebViewReference webViewReference,
            DiscordChatClient chatClient)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _backgroundParking = backgroundParking ?? throw new ArgumentNullException(nameof(backgroundParking));
            _webViewReference = webViewReference ?? throw new ArgumentNullException(nameof(webViewReference));
            _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
            if (webViewFactory == null) throw new ArgumentNullException(nameof(webViewFactory));

            _lifecycle = new DiscordWebViewLifecycle<WebView2>(
                webViewFactory.Create,
                InitializeWebViewAsync,
                webView => webView.CoreWebView2 != null,
                ReadCurrentUrl,
                ReleaseWebView,
                initialUrlProvider);
            _host.Loaded += OnHostLoaded;
            ShowHostMessage("Discord tab is inactive.", Brushes.Gray);
        }

        public bool IsActive => _lifecycle.Current != null && _chatClient.IsReady;

        public static DiscordWebViewSession Create(
            ContentControl host,
            Panel backgroundParking,
            string tabRole,
            Func<string> initialUrlProvider,
            Action<string> telemetry,
            out DiscordChatClient chatClient)
        {
            return Create(host, backgroundParking, tabRole, initialUrlProvider, telemetry,
                string.Empty, "Default", out chatClient);
        }

        public static DiscordWebViewSession Create(
            ContentControl host,
            Panel backgroundParking,
            string tabRole,
            Func<string> initialUrlProvider,
            Action<string> telemetry,
            string accountId,
            string profileName,
            out DiscordChatClient chatClient)
        {
            return Create(
                host,
                backgroundParking,
                tabRole,
                initialUrlProvider,
                telemetry,
                new DiscordWebViewFactory(),
                accountId,
                profileName,
                out chatClient);
        }

        internal static DiscordWebViewSession Create(
            ContentControl host,
            Panel backgroundParking,
            string tabRole,
            Func<string> initialUrlProvider,
            Action<string> telemetry,
            IDiscordWebViewFactory webViewFactory,
            out DiscordChatClient chatClient)
        {
            return Create(host, backgroundParking, tabRole, initialUrlProvider, telemetry,
                webViewFactory, string.Empty, "Default", out chatClient);
        }

        internal static DiscordWebViewSession Create(
            ContentControl host,
            Panel backgroundParking,
            string tabRole,
            Func<string> initialUrlProvider,
            Action<string> telemetry,
            IDiscordWebViewFactory webViewFactory,
            string accountId,
            string profileName,
            out DiscordChatClient chatClient)
        {
            var webViewReference = new DiscordWebViewReference();
            chatClient = new DiscordChatClient(
                webViewReference, tabRole, telemetry, accountId, profileName);
            return new DiscordWebViewSession(
                host,
                backgroundParking,
                initialUrlProvider,
                webViewFactory,
                webViewReference,
                chatClient);
        }

        public async Task SetDemandAsync(
            DiscordWebViewActivityReason reason,
            bool isRequired,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (isRequired && !IsActive)
            {
                ShowHostMessage("Loading Discord...", Brushes.Gray);
            }

            try
            {
                await _lifecycle.SetDemandAsync(reason, isRequired, cancellationToken);
                RefreshPlacementAndStatus();
            }
            catch (Exception ex)
            {
                ShowHostMessage("Discord failed to load: " + ex.Message, Brushes.IndianRed);
                throw;
            }
        }

        public async Task<DiscordWebViewLease> AcquireAsync(
            DiscordWebViewActivityReason reason,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (!IsActive)
            {
                ShowHostMessage("Loading Discord...", Brushes.Gray);
            }

            try
            {
                await _lifecycle.AcquireAsync(reason, cancellationToken);
                PlaceCurrentWebView();
                return new DiscordWebViewLease(this, reason);
            }
            catch (Exception ex)
            {
                ShowHostMessage("Discord failed to load: " + ex.Message, Brushes.IndianRed);
                throw;
            }
        }

        internal async Task ReleaseLeaseAsync(DiscordWebViewActivityReason reason)
        {
            await _lifecycle.ReleaseAsync(reason);
            if (!_disposed)
            {
                RefreshPlacementAndStatus();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _host.Loaded -= OnHostLoaded;
            _lifecycle.Dispose();
        }

        private void RefreshPlacementAndStatus()
        {
            if (_lifecycle.Current != null)
            {
                PlaceCurrentWebView();
            }
            else if (!_lifecycle.HasDemand)
            {
                ShowHostMessage("Discord tab is inactive.", Brushes.Gray);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DiscordWebViewSession));
        }
    }
}
