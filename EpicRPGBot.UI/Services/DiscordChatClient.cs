using System;
using System.IO;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient : IDiscordChatClient, IDuelDiscordClient, IDiscordAttachmentImageClient
    {
        private readonly IDiscordWebViewReference _webViewReference;
        private readonly DiscordTabRole _tabRole;
        private readonly string _accountId;
        private readonly string _profileName;
        private readonly Action<string> _telemetry;
        private WebView2 _configuredWebView;
        private bool _navigationHandlerAttached;
        private bool _roleMarkerRegistered;

        private WebView2 _web => _webViewReference.Current ??
            throw new InvalidOperationException("Discord WebView is not active.");

        public DiscordChatClient(WebView2 web)
            : this(web, "bot", null)
        {
        }

        public DiscordChatClient(WebView2 web, string tabRole, Action<string> telemetry = null)
            : this(new FixedDiscordWebViewReference(web), tabRole, telemetry, string.Empty, "Default")
        {
        }

        internal DiscordChatClient(
            IDiscordWebViewReference webViewReference,
            string tabRole,
            Action<string> telemetry = null,
            string accountId = "",
            string profileName = "Default")
        {
            _webViewReference = webViewReference ?? throw new ArgumentNullException(nameof(webViewReference));
            _tabRole = DiscordTabRoleCatalog.Parse(tabRole);
            if (_tabRole == DiscordTabRole.Unknown)
            {
                _tabRole = DiscordTabRole.Bot;
            }
            _telemetry = telemetry;
            _accountId = accountId ?? string.Empty;
            _profileName = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName;
        }

        public bool IsReady => _webViewReference.Current?.CoreWebView2 != null;

        public async Task EnsureInitializedAsync()
        {
            var webView = _web;
            PrepareWebViewGeneration(webView);
            if (webView.CoreWebView2 == null)
            {
                var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicRPGBot.UI", "WebView2");
                Directory.CreateDirectory(dataDir);
                var env = await WebViewEnvironmentFactory.CreateAsync(dataDir);
                var controllerOptions = env.CreateCoreWebView2ControllerOptions();
                controllerOptions.ProfileName = _profileName;
                await webView.EnsureCoreWebView2Async(env, controllerOptions);
            }

            ConfigureSettings();
            await EnsureRoleMarkerAsync();
            AttachNavigationHandler();
        }

        internal void ReleaseWebView(WebView2 webView)
        {
            if (!ReferenceEquals(_configuredWebView, webView))
            {
                return;
            }

            if (_navigationHandlerAttached)
            {
                webView.NavigationCompleted -= OnNavigationCompleted;
            }

            _configuredWebView = null;
            _navigationHandlerAttached = false;
            _roleMarkerRegistered = false;
        }

        private void PrepareWebViewGeneration(WebView2 webView)
        {
            if (ReferenceEquals(_configuredWebView, webView))
            {
                return;
            }

            if (_configuredWebView != null && _navigationHandlerAttached)
            {
                _configuredWebView.NavigationCompleted -= OnNavigationCompleted;
            }

            _configuredWebView = webView;
            _navigationHandlerAttached = false;
            _roleMarkerRegistered = false;
        }

        public void Reload()
        {
            if (_web.CoreWebView2 != null)
            {
                _web.CoreWebView2.Reload();
            }
            else
            {
                _web.Reload();
            }
        }

        public Task NavigateToChannelAsync(string url)
        {
            if (_web.CoreWebView2 != null)
            {
                _web.CoreWebView2.Navigate(url);
            }
            else
            {
                _web.Source = new Uri(url);
            }

            return Task.CompletedTask;
        }

        private void ConfigureSettings()
        {
            _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            _web.CoreWebView2.Settings.AreDevToolsEnabled = true;
            _web.CoreWebView2.Settings.IsZoomControlEnabled = true;
        }

        private void AttachNavigationHandler()
        {
            if (_navigationHandlerAttached)
            {
                return;
            }

            _web.NavigationCompleted += OnNavigationCompleted;
            _navigationHandlerAttached = true;
        }

        private async Task EnsureRoleMarkerAsync()
        {
            if (_roleMarkerRegistered || _web.CoreWebView2 == null)
            {
                return;
            }

            await _web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(BuildRoleMarkerScript());
            _roleMarkerRegistered = true;
        }

        private async void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            try
            {
                await TagCurrentDocumentAsync();
                await ClickInterstitialsAsync();
            }
            catch
            {
            }
        }

        private Task TagCurrentDocumentAsync()
        {
            return _web.CoreWebView2 == null
                ? Task.CompletedTask
                : _web.CoreWebView2.ExecuteScriptAsync(BuildRoleMarkerScript());
        }

        private string BuildRoleMarkerScript()
        {
            var role = EscapeJavaScriptString(DiscordTabRoleCatalog.ToMarker(_tabRole));
            var accountId = EscapeJavaScriptString(_accountId);
            return $@"
(() => {{
  window.__epicRpGBotTabRole = '{role}';
  window.__epicRpGBotAccountId = '{accountId}';
  try {{
    document.documentElement.setAttribute('data-epicrpg-tab-role', '{role}');
    document.documentElement.setAttribute('data-epicrpg-account-id', '{accountId}');
  }} catch (e) {{}}
}})();
";
        }

        private static string EscapeJavaScriptString(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }

        private void ReportTelemetry(string message)
        {
            _telemetry?.Invoke(message);
        }

        private async Task ClickInterstitialsAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                return;
            }

            const string script = @"
(() => {
  const byText = (txt) => {
    const walker = document.createTreeWalker(document.body, NodeFilter.SHOW_ELEMENT, null);
    let clicked = false;
    while (walker.nextNode()) {
      const el = walker.currentNode;
      if (!(el instanceof HTMLElement)) continue;
      const t = (el.innerText || '').trim();
      if (!t) continue;
      if (t.includes(txt)) {
        try { el.click(); clicked = true; } catch {}
      }
    }
    return clicked;
  };
  let any = false;
  any = byText('Open Discord in your browser') || any;
  any = byText('Continue to Discord') || any;
  any = byText('Continue in browser') || any;
  return any;
})();
";

            await _web.CoreWebView2.ExecuteScriptAsync(script);
        }
    }
}
