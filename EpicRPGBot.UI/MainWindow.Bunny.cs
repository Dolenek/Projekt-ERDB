using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void WireBunnyEvents(BotEngine engine)
        {
            engine.OnBunnyInfo += message =>
            {
                UiDispatcher.OnUI(() => _log.Info("[bunny] " + message));
            };

            engine.OnBunnyAlert += message =>
            {
                UiDispatcher.OnUI(() =>
                {
                    _log.Warning("[bunny] " + message);
                    _alertService.ShowBunnyAlert(this, message);
                });
            };
        }
    }
}
