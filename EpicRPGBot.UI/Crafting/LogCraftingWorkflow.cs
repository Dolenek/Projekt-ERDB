using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Services;

namespace EpicRPGBot.UI.Crafting
{
    public sealed class LogCraftingWorkflow
    {
        private readonly CraftPlanBuilder _planBuilder;
        private readonly CraftReplyParser _replyParser;
        private readonly ConfirmedCommandSender _confirmedCommandSender;

        public LogCraftingWorkflow(ConfirmedCommandSender confirmedCommandSender)
            : this(
                  confirmedCommandSender,
                  new CraftPlanBuilder(CraftItemCatalog.LogChain),
                  new CraftReplyParser(CraftItemCatalog.LogChain))
        {
        }

        public LogCraftingWorkflow(
            ConfirmedCommandSender confirmedCommandSender,
            CraftPlanBuilder planBuilder,
            CraftReplyParser replyParser)
        {
            _confirmedCommandSender = confirmedCommandSender ?? throw new ArgumentNullException(nameof(confirmedCommandSender));
            _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
            _replyParser = replyParser ?? throw new ArgumentNullException(nameof(replyParser));
        }

        public async Task<CraftJobResult> RunAsync(CraftRequest request, Action<string> report, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!request.HasAnyAmount)
            {
                return CraftJobResult.FailedResult("Enter at least one craft amount.");
            }

            var plan = _planBuilder.Build(request);
            report?.Invoke($"Craft plan ready with {plan.Steps.Count} step(s).");

            foreach (var step in plan.Steps)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return CraftJobResult.CancelledResult("Crafting cancelled.");
                }

                var command = $"rpg craft {step.Definition.CommandName} {step.Amount}";
                report?.Invoke($"Sending {command}");

                var result = await _confirmedCommandSender.SendAsync(command);
                if (!result.IsConfirmed || result.ReplyMessage == null)
                {
                    return CraftJobResult.FailedResult($"Crafting stopped: no confirmed EPIC RPG reply for {step.Definition.DisplayName}.");
                }

                var reply = _replyParser.Parse(result.ReplyMessage.Text);
                if (reply.Kind == CraftReplyKind.Success)
                {
                    report?.Invoke($"Crafted {step.Amount} {step.Definition.DisplayName}.");
                    continue;
                }

                if (reply.Kind == CraftReplyKind.MissingItems && reply.MissingItem != null)
                {
                    return CreateMissingItemsResult(step, reply);
                }

                return CraftJobResult.FailedResult($"Crafting stopped: unrecognized reply for {step.Definition.DisplayName}.");
            }

            return CraftJobResult.CompletedResult("Crafting completed.");
        }

        private static CraftJobResult CreateMissingItemsResult(CraftPlanStep step, CraftReply reply)
        {
            var available = reply.AvailableAmount.ToString("N0");
            var required = reply.RequiredAmount.ToString("N0");
            if (!reply.MissingItem.IsCraftable)
            {
                return CraftJobResult.FailedResult(
                    $"Crafting stopped: not enough {reply.MissingItem.DisplayName} ({available}/{required}) to finish {step.Definition.DisplayName}.");
            }

            return CraftJobResult.FailedResult(
                $"Crafting stopped: expected {step.Definition.DisplayName}, but EPIC RPG still reported missing {reply.MissingItem.DisplayName} ({available}/{required}).");
        }
    }
}
