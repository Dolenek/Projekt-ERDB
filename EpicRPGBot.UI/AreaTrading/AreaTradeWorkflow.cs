using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Dismantling;
using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.AreaTrading
{
    public sealed class AreaTradeWorkflow
    {
        private const int CooldownRetryDelayMs = 1000;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly DismantlingWorkflow _dismantlingWorkflow;
        private readonly AppSettingsService _settingsService;
        private readonly Func<AppSettingsSnapshot> _getCurrentSettings;
        private readonly AreaTradePlanCatalog _planCatalog;
        private readonly TradeReplyParser _tradeReplyParser;

        public AreaTradeWorkflow(
            ConfirmedCommandSender confirmedCommandSender,
            DismantlingWorkflow dismantlingWorkflow,
            AppSettingsService settingsService,
            Func<AppSettingsSnapshot> getCurrentSettings)
            : this(
                confirmedCommandSender,
                dismantlingWorkflow,
                settingsService,
                getCurrentSettings,
                AreaTradePlanCatalog.Default,
                new TradeReplyParser())
        {
        }

        public AreaTradeWorkflow(
            ConfirmedCommandSender confirmedCommandSender,
            DismantlingWorkflow dismantlingWorkflow,
            AppSettingsService settingsService,
            Func<AppSettingsSnapshot> getCurrentSettings,
            AreaTradePlanCatalog planCatalog,
            TradeReplyParser tradeReplyParser)
        {
            _confirmedCommandSender = confirmedCommandSender ?? throw new ArgumentNullException(nameof(confirmedCommandSender));
            _dismantlingWorkflow = dismantlingWorkflow ?? throw new ArgumentNullException(nameof(dismantlingWorkflow));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _getCurrentSettings = getCurrentSettings ?? throw new ArgumentNullException(nameof(getCurrentSettings));
            _planCatalog = planCatalog ?? throw new ArgumentNullException(nameof(planCatalog));
            _tradeReplyParser = tradeReplyParser ?? throw new ArgumentNullException(nameof(tradeReplyParser));
        }

        public async Task<CraftJobResult> RunAsync(Action<string> report, CancellationToken cancellationToken)
        {
            report?.Invoke("Refreshing live area from rpg p.");
            var area = await ResolveAreaAsync(report);
            if (!area.HasValue)
            {
                return CraftJobResult.FailedResult("Area trade stopped: failed to resolve live area.");
            }

            if (!_planCatalog.TryGet(area.Value, out var steps) || steps.Count == 0)
            {
                return CraftJobResult.FailedResult($"Area trade stopped: no trade plan exists for area {area.Value}.");
            }

            report?.Invoke($"Area {area.Value} trade plan ready with {steps.Count} step(s).");
            for (var i = 0; i < steps.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return CraftJobResult.CancelledResult("Area trade cancelled.");
                }

                var step = steps[i];
                var result = step.Kind == AreaTradeStepKind.DismantleAll
                    ? await RunDismantleStepAsync(step, report, cancellationToken)
                    : await RunTradeStepAsync(step, report, cancellationToken);

                if (result.Completed)
                {
                    continue;
                }

                if (result.Cancelled)
                {
                    return result;
                }

                if (step.Kind == AreaTradeStepKind.DismantleAll)
                {
                    report?.Invoke($"{result.Summary} Continuing to the next area-trade step.");
                    continue;
                }

                return result;
            }

            return CraftJobResult.CompletedResult("Area trade completed.");
        }

        private async Task<int?> ResolveAreaAsync(Action<string> report)
        {
            var result = await _confirmedCommandSender.SendAsync("rpg p");
            if (!result.IsConfirmed || result.ReplyMessage == null)
            {
                report?.Invoke("Area trade stopped: no confirmed EPIC RPG profile reply.");
                return null;
            }

            if (!ProfileMessageParser.TryParseMaxArea(result.ReplyMessage.Text, out var area))
            {
                report?.Invoke("Area trade stopped: failed to parse max area from the profile reply.");
                return null;
            }

            SaveResolvedArea(area, report);
            return area;
        }

        private void SaveResolvedArea(int area, Action<string> report)
        {
            var currentSettings = _getCurrentSettings();
            if (currentSettings.GetAreaOrDefault(area) == area)
            {
                return;
            }

            _settingsService.Save(currentSettings.WithArea(area.ToString(CultureInfo.InvariantCulture)));
            report?.Invoke($"Updated saved area to {area} from the live profile.");
        }

        private async Task<CraftJobResult> RunDismantleStepAsync(
            AreaTradeStep step,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var request = new DismantleRequest(step.ItemKey.Value, true, 0);
            return await _dismantlingWorkflow.RunAsync(
                request,
                message => report?.Invoke("[dismantle] " + message),
                cancellationToken);
        }

        private async Task<CraftJobResult> RunTradeStepAsync(
            AreaTradeStep step,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            var result = await ExecuteTradeCommandAsync($"rpg trade {step.TradeLetter} all", step, report, cancellationToken);
            if (result.Completed || result.Cancelled)
            {
                return result;
            }

            report?.Invoke($"Trade {step.TradeLetter} failed. Continuing to the next trade.");
            return CraftJobResult.CompletedResult("Trade step failed and was skipped.");
        }

        private async Task<CraftJobResult> ExecuteTradeCommandAsync(
            string command,
            AreaTradeStep step,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                report?.Invoke($"Sending {command}");

                var result = await _confirmedCommandSender.SendAsync(command);
                if (!result.IsConfirmed || result.ReplyMessage == null)
                {
                    return CraftJobResult.FailedResult($"Area trade stopped: no confirmed EPIC RPG reply for trade {step.TradeLetter}.");
                }

                var reply = _tradeReplyParser.Parse(result.ReplyMessage.Text);
                if (reply.Kind == TradeReplyKind.Success)
                {
                    report?.Invoke($"Trade {step.TradeLetter} completed.");
                    return CraftJobResult.CompletedResult("Trade step completed.");
                }

                if (reply.Kind == TradeReplyKind.MissingItems)
                {
                    report?.Invoke($"Trade {step.TradeLetter} skipped: nothing available to trade.");
                    return CraftJobResult.CompletedResult("Trade step skipped.");
                }

                if (reply.Kind == TradeReplyKind.InvalidAmount)
                {
                    report?.Invoke($"Trade {step.TradeLetter} was rejected because the amount must be 1 or higher.");
                    return CraftJobResult.FailedResult("Trade step failed.");
                }

                if (reply.Kind == TradeReplyKind.Wait)
                {
                    report?.Invoke($"Trade cooldown hit for {step.TradeLetter}. Waiting 1 second and retrying.");
                    await Task.Delay(CooldownRetryDelayMs, cancellationToken);
                    continue;
                }

                return CraftJobResult.FailedResult($"Trade {step.TradeLetter} returned an unrecognized reply.");
            }
        }
    }
}
