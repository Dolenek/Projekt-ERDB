using EpicRPGBot.UI.Automation;
using EpicRPGBot.UI.Accounts;
using System;
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

            try
            {
                var window = new MainWindow();
                MainWindow = window;
                window.Show();
            }
            catch (Exception exception) when (AccountRegistryErrorPresenter.IsStorageError(exception))
            {
                AccountRegistryErrorPresenter.Show(null, exception);
                Shutdown(1);
            }
        }
    }
}
