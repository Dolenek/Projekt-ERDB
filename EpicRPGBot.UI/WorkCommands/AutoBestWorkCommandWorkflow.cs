using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.WorkCommands
{
    public sealed partial class AutoBestWorkCommandWorkflow
    {
        private const int BetweenCommandsDelayMs = 1000;

        private readonly Func<string, Action<DiscordMessageSnapshot>, CancellationToken,
            Task<ConfirmedCommandSendResult>> _sendCommand;
        private readonly Func<int, CancellationToken, Task> _delay;

        public AutoBestWorkCommandWorkflow(IDiscordChatClient chatClient)
            : this(
                CreateCommandSender(chatClient),
                (milliseconds, token) => Task.Delay(milliseconds, token))
        {
        }

        internal AutoBestWorkCommandWorkflow(
            Func<string, Action<DiscordMessageSnapshot>, CancellationToken,
                Task<ConfirmedCommandSendResult>> sendCommand,
            Func<int, CancellationToken, Task> delay)
        {
            _sendCommand = sendCommand ?? throw new ArgumentNullException(nameof(sendCommand));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        }

        public async Task<AutoBestWorkCommandResult> RunAsync(
            bool ascended,
            Action<string, DiscordMessageSnapshot> onOutgoingRegistered,
            CancellationToken cancellationToken)
        {
            var profileReply = await TrySendAsync("rpg p", onOutgoingRegistered, cancellationToken);
            if (!TryParseProfile(profileReply, out var profile))
            {
                return AutoBestWorkCommandResult.Failure(
                    "Auto-Best could not read Coins, Bank, and Time travels from the profile.");
            }

            if (!ascended)
            {
                return BuildRegularResult(profile, false, string.Empty);
            }

            await DelayBeforeNextCommandAsync(cancellationToken);
            var professionReply = await TrySendAsync("rpg pr", onOutgoingRegistered, cancellationToken);
            if (!TryParseWorkerLevel(professionReply, out var workerLevel) || workerLevel < 100)
            {
                return BuildRegularResult(
                    profile,
                    true,
                    "Worker level was unavailable or below 100; regular recommendations were used.");
            }

            return await BuildAscendedResultAsync(
                profile,
                workerLevel,
                onOutgoingRegistered,
                cancellationToken);
        }

        private async Task<AutoBestWorkCommandResult> BuildAscendedResultAsync(
            AutoBestWorkProfile profile,
            int workerLevel,
            Action<string, DiscordMessageSnapshot> onOutgoingRegistered,
            CancellationToken cancellationToken)
        {
            await DelayBeforeNextCommandAsync(cancellationToken);
            var boostReply = await TrySendAsync("rpg boost", onOutgoingRegistered, cancellationToken);
            var boostParsed = TryParsePotions(
                boostReply,
                out var fishPotionActive,
                out var woodPotionActive);
            var plan = AutoBestWorkCommandPlanner.Build(
                profile.TotalCoins,
                profile.TimeTravels,
                true,
                workerLevel,
                boostParsed && fishPotionActive,
                boostParsed && woodPotionActive);
            var suffix = boostParsed
                ? string.Empty
                : " Boost status was unavailable; the no-potions table was used.";
            var message = $"Auto-Best applied ascended recommendations for Worker Lv {workerLevel}." + suffix;
            return AutoBestWorkCommandResult.Applied(
                plan.Selections,
                message,
                plan.Mode,
                !boostParsed);
        }

        private static AutoBestWorkCommandResult BuildRegularResult(
            AutoBestWorkProfile profile,
            bool usedFallback,
            string fallbackMessage)
        {
            var plan = AutoBestWorkCommandPlanner.Build(
                profile.TotalCoins,
                profile.TimeTravels,
                false,
                null,
                false,
                false);
            var message = usedFallback
                ? fallbackMessage
                : $"Auto-Best applied regular recommendations for {profile.TimeTravels} time travels.";
            return AutoBestWorkCommandResult.Applied(
                plan.Selections,
                message,
                plan.Mode,
                usedFallback);
        }

        private async Task<ConfirmedCommandSendResult> SendAsync(
            string command,
            Action<string, DiscordMessageSnapshot> onOutgoingRegistered,
            CancellationToken cancellationToken)
        {
            return await _sendCommand(
                command,
                snapshot => onOutgoingRegistered?.Invoke(command, snapshot),
                cancellationToken);
        }

        private async Task<ConfirmedCommandSendResult> TrySendAsync(
            string command,
            Action<string, DiscordMessageSnapshot> onOutgoingRegistered,
            CancellationToken cancellationToken)
        {
            try
            {
                return await SendAsync(command, onOutgoingRegistered, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                return null;
            }
        }

        private Task DelayBeforeNextCommandAsync(CancellationToken cancellationToken)
        {
            return _delay(BetweenCommandsDelayMs, cancellationToken);
        }

        private static Func<string, Action<DiscordMessageSnapshot>, CancellationToken,
            Task<ConfirmedCommandSendResult>> CreateCommandSender(IDiscordChatClient chatClient)
        {
            if (chatClient == null) throw new ArgumentNullException(nameof(chatClient));
            var commandSender = new ConfirmedCommandSender(chatClient);
            return (command, callback, token) => commandSender.SendAsync(command, callback, token);
        }
    }
}
