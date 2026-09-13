using Microsoft.Web.WebView2.Wpf;

namespace EpicRPGBot.UI.Services
{
    internal interface IDiscordWebViewFactory
    {
        WebView2 Create();
    }

    internal sealed class DiscordWebViewFactory : IDiscordWebViewFactory
    {
        public WebView2 Create()
        {
            return new WebView2
            {
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch
            };
        }
    }
}
