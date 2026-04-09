using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;
using EpicRPGBot.UI.TimeCookie;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private const string TimeCookieOperationName = "time cookie";

        private async void TimeCookieDungeonBtn_Click(object sender, RoutedEventArgs e)
        {
            await HandleTimeCookieTargetClickAsync(TimeCookieTarget.Dungeon);
        }

        private async void TimeCookieDuelBtn_Click(object sender, RoutedEventArgs e)
        {
            await HandleTimeCookieTargetClickAsync(TimeCookieTarget.Duel);
        }

        private async void TimeCookieCardHandBtn_Click(object sender, RoutedEventArgs e)
        {
            await HandleTimeCookieTargetClickAsync(TimeCookieTarget.CardHand);
        }

        private async Task HandleTimeCookieTargetClickAsync(TimeCookieTarget target)
        {
            if (_isTimeCookieRunning)
            {
                if (_activeTimeCookieTarget == target)
                {
                    RequestTimeCookieStop();
                }

                return;
            }

            if (!TryBeginExclusiveBotOperation(TimeCookieOperationName))
            {
                return;
            }

            if (!_botChatClient.IsReady)
            {
                _log.Info("WebView2 not ready");
                EndExclusiveBotOperation(TimeCookieOperationName);
                return;
            }

            await RunTimeCookieLoopAsync(target);
        }

        private async Task RunTimeCookieLoopAsync(TimeCookieTarget target)
        {
            var targetDefinition = TimeCookieTargetCatalog.Get(target);
            var engineWasRunning = _engine != null && _engine.IsRunning;
            SetTimeCookieRunning(target, true);
            _timeCookieCancellation = new CancellationTokenSource();
            _log.Info($"[time cookie] Started for {targetDefinition.DisplayName}.");

            try
            {
                var hasFreshSnapshot = false;
                if (!engineWasRunning)
                {
                    await StartEngineAsync("Engine started for time cookie");
                    var startupSnapshot = await SendExclusiveEngineCommandAsync("rpg cd", armStartupCutoff: true);
                    if (!startupSnapshot.IsConfirmed)
                    {
                        _log.Info("[time cookie] Stopped: failed to refresh cooldowns after starting the engine.");
                        return;
                    }

                    hasFreshSnapshot = true;
                }

                await RunTimeCookieLoopCoreAsync(targetDefinition, hasFreshSnapshot, _timeCookieCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                _log.Info("[time cookie] Stopped by user.");
            }
            catch (Exception ex)
            {
                _log.Warning("[time cookie] failed: " + ex.Message);
            }
            finally
            {
                _timeCookieCancellation.Dispose();
                _timeCookieCancellation = null;
                SetTimeCookieRunning(target, false);
                EndExclusiveBotOperation(TimeCookieOperationName);

                if (!engineWasRunning && _engine != null && _engine.IsRunning)
                {
                    await _engine.StopAsync();
                    _log.Engine("Engine stopped after time cookie");
                }
            }
        }

        private async Task RunTimeCookieLoopCoreAsync(
            TimeCookieTargetDefinition targetDefinition,
            bool hasFreshSnapshot,
            CancellationToken cancellationToken)
        {
            var hasSnapshotForCycle = hasFreshSnapshot;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!hasSnapshotForCycle)
                {
                    _log.Info("[time cookie] Refreshing cooldowns with 'rpg cd'.");
                    var cdResult = await SendExclusiveEngineCommandAsync("rpg cd");
                    if (!cdResult.IsConfirmed)
                    {
                        _log.Info("[time cookie] Stopped: 'rpg cd' did not confirm.");
                        return;
                    }
                }

                hasSnapshotForCycle = false;
                if (!await WaitForAutomatedCommandsToSettleAsync(cancellationToken))
                {
                    _log.Info("[time cookie] Stopped: automated cooldown batch did not settle in time.");
                    return;
                }

                var loopStatus = GetTimeCookieLoopStatus(targetDefinition);
                if (loopStatus.IsTargetReady)
                {
                    _log.Info($"[time cookie] {targetDefinition.DisplayName} is ready. Leaving it for manual use.");
                    return;
                }

                if (!loopStatus.CanUseTimeCookie)
                {
                    continue;
                }

                _log.Info("[time cookie] Sending 'rpg use time cookie'.");
                var timeCookieResult = await SendExclusiveEngineCommandAsync("rpg use time cookie");
                if (!timeCookieResult.IsConfirmed)
                {
                    _log.Info("[time cookie] Stopped: 'rpg use time cookie' did not confirm.");
                    return;
                }

                if (!TimeCookieMessageParser.TryParseReduction(timeCookieResult.ReplyMessage?.Text, out var reduction))
                {
                    _log.Info("[time cookie] Stopped: time cookie reply did not include a recognized cooldown reduction.");
                    return;
                }

                _log.Info($"[time cookie] Applied {reduction.TotalMinutes:0} minute(s) of cooldown reduction.");
                if (!await WaitForAutomatedCommandsToSettleAsync(cancellationToken))
                {
                    _log.Info("[time cookie] Stopped: automated cooldown batch did not settle after the time cookie.");
                    return;
                }
            }
        }

        private TimeCookieLoopStatus GetTimeCookieLoopStatus(TimeCookieTargetDefinition targetDefinition)
        {
            return TimeCookieLoopEvaluator.Evaluate(
                _cooldownTracker.GetRemaining(targetDefinition.CanonicalCooldownKey),
                _cooldownTracker.GetTrackedSnapshot(),
                IsFarmAllowedForConfiguredArea());
        }

        private void RequestTimeCookieStop()
        {
            if (_timeCookieCancellation == null || _timeCookieCancellation.IsCancellationRequested)
            {
                return;
            }

            _log.Info("[time cookie] Stop requested.");
            _timeCookieCancellation.Cancel();
        }

        private void SetTimeCookieRunning(TimeCookieTarget target, bool isRunning)
        {
            _isTimeCookieRunning = isRunning;
            _activeTimeCookieTarget = isRunning ? target : (TimeCookieTarget?)null;
            RefreshBotControlButtonColors();
        }
    }
}
