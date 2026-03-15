using EpicRPGBot.UI.Automation;
using System.Windows;

namespace EpicRPGBot.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AutomationRuntime.Initialize(AutomationOptions.Parse(e.Args));
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
