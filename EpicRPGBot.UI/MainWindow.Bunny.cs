using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void WireBunnyEvents(BotEngine engine)
        {
            engine.OnBunnyInfo += (message, reference) =>
            {
                UiDispatcher.OnUI(() => _log.Info("[pet] " + message, reference));
            };

            engine.OnBunnyAlert += (message, reference) =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Warning("[pet] " + message, reference);
                    _alertService.ShowBunnyAlert(this, message);
                });
            };
        }
    }
}
