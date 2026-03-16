using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI.Services
{
    public sealed partial class DiscordChatClient : IDiscordChatClient
    {
        private readonly WebView2 _web;
        private readonly string _tabRole;
        private bool _navigationHandlerAttached;
        private bool _roleMarkerRegistered;

        public DiscordChatClient(WebView2 web)
            : this(web, "bot")
        {
        }

        public DiscordChatClient(WebView2 web, string tabRole)
        {
            _web = web ?? throw new ArgumentNullException(nameof(web));
            _tabRole = string.IsNullOrWhiteSpace(tabRole) ? "bot" : tabRole.Trim().ToLowerInvariant();
        }

        public bool IsReady => _web.CoreWebView2 != null;

        public async Task EnsureInitializedAsync()
        {
            if (_web.CoreWebView2 == null)
            {
                var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EpicRPGBot.UI", "WebView2");
                Directory.CreateDirectory(dataDir);
                var env = await WebViewEnvironmentFactory.CreateAsync(dataDir);
                await _web.EnsureCoreWebView2Async(env);
            }

            ConfigureSettings();
            await EnsureRoleMarkerAsync();
            AttachNavigationHandler();
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
            var role = EscapeJavaScriptString(_tabRole);
            return $@"
(() => {{
  window.__epicRpGBotTabRole = '{role}';
  try {{
    document.documentElement.setAttribute('data-epicrpg-tab-role', '{role}');
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
