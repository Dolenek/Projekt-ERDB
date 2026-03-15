using System.Threading.Tasks;
using EpicRPGBot.UI.Automation;
using Microsoft.Web.WebView2.Core;

namespace EpicRPGBot.UI.Services
{
    internal static class WebViewEnvironmentFactory
    {
        public static Task<CoreWebView2Environment> CreateAsync(string userDataFolder)
        {
            var options = CreateOptions();
            return CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
        }

        private static CoreWebView2EnvironmentOptions CreateOptions()
        {
            var automation = AutomationRuntime.Current;
            if (!automation.IsEnabled || automation.DebugPort <= 0)
            {
                return null;
            }

            return new CoreWebView2EnvironmentOptions($"--remote-debugging-port={automation.DebugPort}");
        }
    }
}
