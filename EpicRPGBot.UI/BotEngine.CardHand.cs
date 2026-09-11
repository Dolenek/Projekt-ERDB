using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.CardHand;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI
{
    public sealed partial class BotEngine
    {
        private const int CardHandReplyPollMs = 250;
        private const int CardHandReplyTimeoutMs = 20000;
        private const int CardHandReplyScanCount = 30;
        private bool _cardHandAutomationEnabled = true;

        private async Task RunCardHandAsync()
        {
            var settings = _cardHandSettingsProvider();
            if (!_running || !settings.AutoPlayEnabled) return;
            if (IsGuardIncidentActive)
            {
                _scheduler.Schedule(TrackedCommandKind.CardHand, TimeSpan.FromSeconds(5), _running);
                return;
            }

            if (!_interactivePromptGate.TryBeginCardHand()) return;
            var acquired = false;
            try
            {
                await _sendGate.WaitAsync(_stopCancellation.Token);
                acquired = true;
                await PlayCardHandInSendLaneAsync(settings, _stopCancellation.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                StopForCardHandFailure("Card hand failed: " + ex.Message);
            }
            finally
            {
                _interactivePromptGate.EndCardHand();
                if (acquired) _sendGate.Release();
            }
        }

        private async Task PlayCardHandInSendLaneAsync(
            CardHandSettingsSnapshot settings,
            CancellationToken cancellationToken)
        {
            await RespectMinimumCommandGapAsync();
            var send = await _confirmedCommandSender.SendAsync(
                "rpg card hand",
                snapshot => RegisterCardHandCommand(),
                cancellationToken);
            if (!send.IsConfirmed)
            {
                RetryCardHandSoon("Card hand command was not confirmed.");
                return;
            }

            OnCommandConfirmed?.Invoke("rpg card hand", send.ReplyMessage);
            ProcessObservedSnapshot(send.ReplyMessage, true);
            if (TrackedCommandResponseClassifier.TryParseWaitAtLeast(send.ReplyMessage.Text, out _)) return;
            if (!settings.IsDeckLoaded)
                ReportCardHandInfo("No deck is loaded; all cards are treated as unowned.");
            await PlayCardHandRoundsAsync(send.ReplyMessage, settings, cancellationToken);
        }

        private void RegisterCardHandCommand()
        {
            _lastCommandSentUtc = DateTime.UtcNow;
            _scheduler.RegisterPending(TrackedCommandKind.CardHand);
            OnCommandSent?.Invoke("rpg card hand");
        }

        private async Task PlayCardHandRoundsAsync(
            DiscordMessageSnapshot firstPrompt,
            CardHandSettingsSnapshot settings,
            CancellationToken cancellationToken)
        {
            var current = await ResolveInitialPromptAsync(firstPrompt, cancellationToken);
            if (current == null)
            {
                StopForCardHandFailure("The initial two-card prompt did not become ready.");
                return;
            }

            var discarded = new List<CardId>();
            CardHandState previousState = null;
            CardHandAction previousAction = null;
            for (var round = 1; round <= 3; round++)
            {
                var parsed = _cardHandPromptParser.Parse(current);
                var state = TryBuildLiveState(parsed, discarded, previousState, previousAction);
                var action = await ChooseOrPassAsync(state, parsed, settings, cancellationToken);
                var clickedAction = await ClickPreferredOrPassAsync(current, parsed, action, cancellationToken);
                if (clickedAction == null) return;
                if (clickedAction.Kind == CardHandActionKind.Discard) discarded.Add(clickedAction.DiscardedCard);

                var next = await WaitForCardHandMessageAsync(
                    current,
                    state,
                    clickedAction,
                    discarded,
                    cancellationToken);
                if (next == null)
                {
                    StopForCardHandFailure("No next card-hand round or result appeared.");
                    return;
                }

                ProcessObservedSnapshot(next, true);
                if (_cardHandPromptParser.IsResult(next))
                {
                    ReportCardHandInfo("Card hand completed successfully.");
                    return;
                }

                previousState = state;
                previousAction = clickedAction;
                current = next;
            }

            if (!_cardHandPromptParser.IsResult(current))
                StopForCardHandFailure("The final card-hand result could not be verified.");
            else
                ReportCardHandInfo("Card hand completed successfully.");
        }

        private async Task<DiscordMessageSnapshot> ResolveInitialPromptAsync(
            DiscordMessageSnapshot initial,
            CancellationToken token)
        {
            var parsed = _cardHandPromptParser.Parse(initial);
            if (parsed.IsValid && parsed.Cards.Count == 2) return initial;
            return await WaitForCardHandMessageAsync(initial, null, null, Array.Empty<CardId>(), token);
        }

        private CardHandState TryBuildLiveState(
            CardHandPromptParseResult parsed,
            IReadOnlyCollection<CardId> discarded,
            CardHandState previousState,
            CardHandAction previousAction)
        {
            if (parsed == null || !parsed.IsValid) return null;
            if (previousState != null && !IsExpectedNextHand(parsed.Cards, previousState, previousAction)) return null;
            if (parsed.Cards.Any(discarded.Contains)) return null;
            try { return new CardHandState(parsed.Cards, discarded); }
            catch (ArgumentException) { return null; }
        }

        private static bool IsExpectedNextHand(
            IReadOnlyCollection<CardId> cards,
            CardHandState previous,
            CardHandAction action)
        {
            if (action == null || cards.Count != previous.Hand.Count + 1) return false;
            var kept = previous.Hand.Where(card => action.Kind != CardHandActionKind.Discard || card != action.DiscardedCard);
            return kept.All(cards.Contains) &&
                   (action.Kind != CardHandActionKind.Discard || !cards.Contains(action.DiscardedCard));
        }

        private async Task<CardHandAction> ChooseOrPassAsync(
            CardHandState state,
            CardHandPromptParseResult prompt,
            CardHandSettingsSnapshot settings,
            CancellationToken token)
        {
            if (state == null)
            {
                ReportCardHandInfo("Prompt validation failed; using pass fallback: " + (prompt?.Error ?? "state mismatch"));
                return CardHandAction.Pass;
            }

            try
            {
                var result = await _cardHandDecisionEngine.DecideAsync(
                    state,
                    settings.OwnedCards,
                    settings.RewardWeights,
                    token);
                ReportDecision(state, result);
                return result?.Action ?? CardHandAction.Pass;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                ReportCardHandInfo("Solver failed; using pass fallback: " + ex.Message);
                return CardHandAction.Pass;
            }
        }

        private async Task<CardHandAction> ClickPreferredOrPassAsync(
            DiscordMessageSnapshot snapshot,
            CardHandPromptParseResult prompt,
            CardHandAction preferred,
            CancellationToken token)
        {
            var button = FindActionButton(prompt, preferred);
            if (button != null && await ClickButtonAsync(snapshot, button, token)) return preferred;
            if (preferred.Kind != CardHandActionKind.Pass)
            {
                ReportCardHandInfo("Preferred card button failed; using pass fallback.");
                if (prompt?.PassButton != null && await ClickButtonAsync(snapshot, prompt.PassButton, token))
                    return CardHandAction.Pass;
            }

            StopForCardHandFailure("The card-hand pass fallback could not be clicked.");
            return null;
        }

        private static DiscordMessageButton FindActionButton(
            CardHandPromptParseResult prompt,
            CardHandAction action)
        {
            if (prompt == null || action == null) return null;
            if (action.Kind == CardHandActionKind.Pass) return prompt.PassButton;
            return prompt.DiscardButtons.TryGetValue(action.DiscardedCard, out var button) ? button : null;
        }

        private Task<bool> ClickButtonAsync(
            DiscordMessageSnapshot snapshot,
            DiscordMessageButton button,
            CancellationToken token)
        {
            return _chatClient.ClickMessageButtonAsync(snapshot.Id, button.RowIndex, button.ColumnIndex, token);
        }

        private async Task<DiscordMessageSnapshot> WaitForCardHandMessageAsync(
            DiscordMessageSnapshot current,
            CardHandState previousState,
            CardHandAction previousAction,
            IReadOnlyCollection<CardId> discarded,
            CancellationToken token)
        {
            var waited = 0;
            var expectedCardCount = previousState?.Hand.Count + 1 ?? 2;
            while (waited < CardHandReplyTimeoutMs)
            {
                await SafeDelay(CardHandReplyPollMs, token);
                waited += CardHandReplyPollMs;
                var messages = await _chatClient.GetRecentMessagesAsync(CardHandReplyScanCount);
                foreach (var candidate in _cardHandMessageSelector.FindCandidates(messages, current, expectedCardCount))
                {
                    if (_cardHandPromptParser.IsResult(candidate)) return candidate;
                    var parsed = _cardHandPromptParser.Parse(candidate);
                    if (TryBuildLiveState(parsed, discarded, previousState, previousAction) != null)
                        return candidate;
                }
            }

            return null;
        }

        private void ReportDecision(CardHandState state, CardHandDecisionResult result)
        {
            var cards = string.Join(" ", state.Hand);
            ReportCardHandInfo(
                $"[{cards}] {result.Action}; EV {result.ExpectedUtility:0.##}; " +
                $"positive {result.PositivePayoutProbability:P1}; {result.SampleCount} terminal samples.");
        }

        private void RetryCardHandSoon(string message)
        {
            _scheduler.ClearPending(TrackedCommandKind.CardHand);
            if (_running) _scheduler.Schedule(TrackedCommandKind.CardHand, TimeSpan.FromSeconds(5), true);
            ReportCardHandInfo(message + " Retrying in 5 seconds.");
        }

        private void StopForCardHandFailure(string message)
        {
            OnCardHandAlert?.Invoke(message);
            Stop();
        }

        private void ReportCardHandInfo(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) OnCardHandInfo?.Invoke(message);
        }

        public void UpdateCardHandSettings(CardHandSettingsSnapshot settings)
        {
            if (settings == null) return;
            var changed = _cardHandAutomationEnabled != settings.AutoPlayEnabled;
            _cardHandAutomationEnabled = settings.AutoPlayEnabled;
            _scheduler.SetCardHandEnabled(settings.AutoPlayEnabled, _running);
            if (changed && settings.AutoPlayEnabled && _running) QueueCooldownSnapshotRequest();
        }

        public async Task<CardDeckImportResult> ImportCardDeckAsync(CardDeckImportWorkflow workflow)
        {
            if (workflow == null) throw new ArgumentNullException(nameof(workflow));
            var acquired = false;
            try
            {
                if (!await WaitForInteractivePromptWindowAsync())
                    return new CardDeckImportResult(false, null, "Card deck loading was cancelled.");
                await _sendGate.WaitAsync(_stopCancellation.Token);
                acquired = true;
                await RespectMinimumCommandGapAsync();
                return await workflow.RunAsync(
                    () =>
                    {
                        _lastCommandSentUtc = DateTime.UtcNow;
                        OnCommandSent?.Invoke("rpg card deck");
                    },
                    _stopCancellation.Token);
            }
            catch (OperationCanceledException)
            {
                return new CardDeckImportResult(false, null, "Card deck loading was cancelled.");
            }
            finally
            {
                if (acquired) _sendGate.Release();
            }
        }
    }
}
