using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using EpicRPGBot.UI.Automation;
using Microsoft.Web.WebView2.Core;

namespace EpicRPGBot.UI.Services
{
    internal static class WebViewEnvironmentFactory
    {
        private static readonly ConcurrentDictionary<string, Task<CoreWebView2Environment>> Environments =
            new ConcurrentDictionary<string, Task<CoreWebView2Environment>>();

        public static Task<CoreWebView2Environment> CreateAsync(string userDataFolder)
        {
            var normalizedFolder = Path.GetFullPath(userDataFolder ?? string.Empty);
            return Environments.GetOrAdd(normalizedFolder, folder =>
            {
                var options = CreateOptions();
                return CoreWebView2Environment.CreateAsync(null, folder, options);
            });
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
