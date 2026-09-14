using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void WireBunnyEvents(BotEngine engine, Accounts.AccountRuntime runtime)
        {
            engine.OnBunnyInfo += (message, reference) =>
            {
                DispatchAccount(runtime, () => _log.Info("[pet] " + message, reference));
            };

            engine.OnBunnyAlert += (message, reference) =>
            {
                DispatchAccount(runtime, () =>
                {
                    _log.Warning("[pet] " + message, reference);
                    _alertService.ShowBunnyAlert(this, $"{runtime.Definition.DisplayName}: {message}");
                });
            };
        }
    }
}
