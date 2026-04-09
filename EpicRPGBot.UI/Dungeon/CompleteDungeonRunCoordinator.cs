using System;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Crafting;

namespace EpicRPGBot.UI.Dungeon
{
    public sealed class CompleteDungeonRunCoordinator
    {
        public async Task<DungeonRunResult> RunAsync(
            Func<Action<string>, CancellationToken, Task<CraftJobResult>> runAreaTradeAsync,
            Func<Action<string>, CancellationToken, Task<DungeonRunResult>> runDungeonAsync,
            Action<string> report,
            CancellationToken cancellationToken)
        {
            if (runAreaTradeAsync == null)
            {
                throw new ArgumentNullException(nameof(runAreaTradeAsync));
            }

            if (runDungeonAsync == null)
            {
                throw new ArgumentNullException(nameof(runDungeonAsync));
            }

            report?.Invoke("[dungeon] Starting mandatory pre-dungeon area trade.");
            var areaTradeResult = await runAreaTradeAsync(report, cancellationToken);
            if (areaTradeResult == null)
            {
                return DungeonRunResult.FailedResult("Dungeon stopped: pre-dungeon area trade returned no result.");
            }

            if (areaTradeResult.Cancelled)
            {
                return DungeonRunResult.CancelledResult("Dungeon cancelled.");
            }

            if (!areaTradeResult.Completed)
            {
                return DungeonRunResult.FailedResult(
                    "Dungeon stopped: pre-dungeon area trade failed. " + areaTradeResult.Summary);
            }

            report?.Invoke("[dungeon] Pre-dungeon area trade completed. Switching to the listing channel.");
            return await runDungeonAsync(report, cancellationToken);
        }
    }
}
