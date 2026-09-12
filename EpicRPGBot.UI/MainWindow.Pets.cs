using System;
using System.Windows;
using EpicRPGBot.UI.Pets;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private async void PetsBtn_Click(object sender, RoutedEventArgs args)
        {
            if (ShouldBlockForExclusiveBotOperation("Pets") || !_botChatClient.IsReady) return;
            if (_engine != null && !_engine.CanOpenPets)
            {
                _log.Info("[pets] Resolve the active interactive command before opening Pets.");
                return;
            }
            if (!TryBeginExclusiveBotOperation("pets")) return;
            var resume = _engine != null && _engine.IsRunning;
            var safe = false;
            try
            {
                if (_engine == null) _engine = CreateEngine();
                if (resume) await _engine.PauseForPetsAsync();
                var gateway = new DiscordPetGateway(_botChatClient, _engine.SendPetCommandAsync,
                    GetCurrentSettings().ProfilePlayerName);
                var window = new PetsWindow(gateway, message => _log.Info("[pets] " + message)) { Owner = this };
                window.ShowDialog();
                safe = window.SafeToResume && !window.UserStopped;
            }
            catch (Exception exception)
            {
                if (_engine != null) await _engine.StopAsync();
                _log.Warning("[pets] " + exception.Message);
            }
            finally
            {
                EndExclusiveBotOperation("pets");
                if (resume && safe) await StartEngineAndRequestCooldownSnapshotAsync("Engine resumed after pets");
            }
        }
    }
}
