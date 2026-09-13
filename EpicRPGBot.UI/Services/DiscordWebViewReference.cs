using System;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI.Services
{
    internal interface IDiscordWebViewReference
    {
        WebView2 Current { get; }
    }

    internal sealed class DiscordWebViewReference : IDiscordWebViewReference
    {
        public WebView2 Current { get; private set; }

        public void Attach(WebView2 webView)
        {
            Current = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        public void Detach(WebView2 webView)
        {
            if (ReferenceEquals(Current, webView))
            {
                Current = null;
            }
        }
    }

    internal sealed class FixedDiscordWebViewReference : IDiscordWebViewReference
    {
        public FixedDiscordWebViewReference(WebView2 webView)
        {
            Current = webView ?? throw new ArgumentNullException(nameof(webView));
        }

        public WebView2 Current { get; }
    }
}
