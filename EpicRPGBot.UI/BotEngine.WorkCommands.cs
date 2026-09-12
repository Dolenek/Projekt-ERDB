using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.WorkCommands;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        public void UpdateWorkCommand(string workCommand)
        {
            _work = NormalizeWorkCommand(workCommand);
        }

        public async Task<AutoBestWorkCommandResult> LoadAutoBestWorkCommandsAsync(
            AutoBestWorkCommandWorkflow workflow,
            bool ascended,
            CancellationToken cancellationToken)
        {
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            using (var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _stopCancellation.Token))
            {
                var acquired = false;
                try
                {
                    await _sendGate.WaitAsync(linkedCancellation.Token);
                    acquired = true;
                    if (IsGuardIncidentActive || IsInteractivePromptPending())
                    {
                        return AutoBestWorkCommandResult.Failure(
                            "Resolve the current interactive command before using Auto-Best.");
                    }

                    await RespectMinimumCommandGapAsync();
                    linkedCancellation.Token.ThrowIfCancellationRequested();
                    return await workflow.RunAsync(
                        ascended,
                        RegisterAutoBestCommand,
                        linkedCancellation.Token);
                }
                catch (OperationCanceledException)
                {
                    return AutoBestWorkCommandResult.Failure("Auto-Best was cancelled.");
                }
                finally
                {
                    if (acquired) _sendGate.Release();
                }
            }
        }

        private void RegisterAutoBestCommand(
            string command,
            Models.DiscordMessageSnapshot snapshot)
        {
            _lastCommandSentUtc = DateTime.UtcNow;
            OnCommandSent?.Invoke(command, snapshot);
        }
    }
}
