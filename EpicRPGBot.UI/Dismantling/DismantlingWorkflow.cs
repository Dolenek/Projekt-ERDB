using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Dismantling
{
    public sealed class DismantlingWorkflow
    {
        private readonly CraftItemCatalog _catalog;
        private readonly ConfirmedCommandSender _confirmedCommandSender;
        private readonly CraftReplyParser _replyParser;

        public DismantlingWorkflow(ConfirmedCommandSender confirmedCommandSender)
            : this(confirmedCommandSender, CraftItemCatalog.Default, new CraftReplyParser(CraftItemCatalog.Default))
        {
        }

        public DismantlingWorkflow(
            ConfirmedCommandSender confirmedCommandSender,
            CraftItemCatalog catalog,
            CraftReplyParser replyParser)
        {
            _confirmedCommandSender = confirmedCommandSender ?? throw new ArgumentNullException(nameof(confirmedCommandSender));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _replyParser = replyParser ?? throw new ArgumentNullException(nameof(replyParser));
        }

        public async Task<CraftJobResult> RunAsync(DismantleRequest request, Action<string> report, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_catalog.TryGet(request.ItemKey, out var selectedItem) || !selectedItem.IsDismantlable)
            {
                return CraftJobResult.FailedResult("Select one dismantlable item.");
            }

            var steps = BuildPlan(selectedItem, request);
            report?.Invoke($"Dismantle plan ready with {steps.Count} step(s).");

            foreach (var step in steps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return CraftJobResult.CancelledResult("Dismantling cancelled.");
                }

                var command = $"rpg dismantle {step.Definition.CommandName} {step.CommandAmount}";
                report?.Invoke($"Sending {command}");

                var result = await _confirmedCommandSender.SendAsync(command);
                if (!result.IsConfirmed || result.ReplyMessage == null)
                {
                    return CraftJobResult.FailedResult($"Dismantling stopped: no confirmed EPIC RPG reply for {step.Definition.DisplayName}.");
                }

                var reply = _replyParser.Parse(result.ReplyMessage.Text);
                if (reply.Kind == CraftReplyKind.Success)
                {
                    report?.Invoke(FormatSuccess(step));
                    continue;
                }

                if (reply.Kind == CraftReplyKind.MissingItems)
                {
                    if (request.UseAll)
                    {
                        report?.Invoke($"No {step.Definition.DisplayName} to dismantle. Continuing downward.");
                        continue;
                    }

                    return CraftJobResult.FailedResult($"Dismantling stopped: not enough {step.Definition.DisplayName} to continue.");
                }

                return CraftJobResult.FailedResult($"Dismantling stopped: unrecognized reply for {step.Definition.DisplayName}.");
            }

            return CraftJobResult.CompletedResult("Dismantling completed.");
        }

        private static string FormatSuccess(DismantlePlanStep step)
        {
            if (step.CommandAmount == "all")
            {
                return $"Dismantled all {step.Definition.DisplayName}.";
            }

            return step.Definition.DismantleOutputKey.HasValue
                ? $"Dismantled {step.CommandAmount} {step.Definition.DisplayName} into {step.ProducedAmount:N0}."
                : $"Dismantled {step.CommandAmount} {step.Definition.DisplayName}.";
        }

        private IReadOnlyList<DismantlePlanStep> BuildPlan(CraftItemDefinition selectedItem, DismantleRequest request)
        {
            var steps = new List<DismantlePlanStep>();
            var currentItem = selectedItem;
            var currentAmount = request.Amount;

            while (currentItem != null && currentItem.IsDismantlable)
            {
                if (request.UseAll)
                {
                    steps.Add(new DismantlePlanStep(currentItem, "all"));
                }
                else
                {
                    checked
                    {
                        var producedAmount = currentAmount * currentItem.DismantleOutputAmount;
                        steps.Add(new DismantlePlanStep(currentItem, currentAmount.ToString(), producedAmount));
                        currentAmount = producedAmount;
                    }
                }

                if (!currentItem.DismantleOutputKey.HasValue || !_catalog.TryGet(currentItem.DismantleOutputKey.Value, out currentItem))
                {
                    break;
                }
            }

            return steps;
        }
    }
}
