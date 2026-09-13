using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordWebViewSession
    {
        private async Task InitializeWebViewAsync(
            WebView2 webView,
            string destination,
            CancellationToken cancellationToken)
        {
            _webViewReference.Attach(webView);
            PlaceCurrentWebView();
            await _chatClient.EnsureInitializedAsync();
            await NavigateInitialUrlAsync(webView, destination, cancellationToken);
        }

        private void OnHostLoaded(object sender, RoutedEventArgs e)
        {
            if (!_disposed && _lifecycle.IsSelected)
            {
                PlaceCurrentWebView();
            }
        }

        private void PlaceCurrentWebView()
        {
            var webView = _lifecycle.Current;
            if (webView == null)
            {
                return;
            }

            DetachWebViewFromHost(webView);
            if (_lifecycle.IsSelected && _host.IsLoaded)
            {
                _host.Content = webView;
            }
            else
            {
                _backgroundParking.Children.Add(webView);
            }
        }

        private void DetachWebViewFromHost(WebView2 webView)
        {
            if (ReferenceEquals(_host.Content, webView))
            {
                _host.Content = null;
            }

            if (_backgroundParking.Children.Contains(webView))
            {
                _backgroundParking.Children.Remove(webView);
            }
        }

        private async Task NavigateInitialUrlAsync(
            WebView2 webView,
            string destination,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                return;
            }

            var navigationCompleted = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<CoreWebView2NavigationCompletedEventArgs> handler =
                (sender, args) => navigationCompleted.TrySetResult(args.IsSuccess);
            webView.NavigationCompleted += handler;
            try
            {
                await _chatClient.NavigateToChannelAsync(destination);
                await WaitForInitialNavigationAsync(navigationCompleted.Task, cancellationToken);
            }
            finally
            {
                webView.NavigationCompleted -= handler;
            }
        }

        private static async Task WaitForInitialNavigationAsync(
            Task<bool> navigationTask,
            CancellationToken cancellationToken)
        {
            using (var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken))
            {
                var timeoutTask = Task.Delay(InitialNavigationTimeoutMs, timeoutCancellation.Token);
                var completedTask = await Task.WhenAny(navigationTask, timeoutTask);
                cancellationToken.ThrowIfCancellationRequested();
                if (completedTask != navigationTask)
                {
                    throw new TimeoutException("Discord navigation timed out.");
                }

                timeoutCancellation.Cancel();
                if (!await navigationTask)
                {
                    throw new InvalidOperationException("Discord navigation failed.");
                }
            }
        }

        private static string ReadCurrentUrl(WebView2 webView)
        {
            return webView.CoreWebView2?.Source ?? string.Empty;
        }

        private void ReleaseWebView(WebView2 webView)
        {
            try
            {
                _chatClient.ReleaseWebView(webView);
            }
            finally
            {
                _webViewReference.Detach(webView);
                DetachWebViewFromHost(webView);
                webView.Dispose();
            }
        }

        private void ShowHostMessage(string message, Brush foreground)
        {
            _host.Content = new TextBlock
            {
                Text = message,
                Foreground = foreground,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(24)
            };
        }
    }
}
