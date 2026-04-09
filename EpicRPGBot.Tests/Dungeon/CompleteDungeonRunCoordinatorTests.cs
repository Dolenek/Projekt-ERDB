using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EpicRPGBot.UI.Crafting;
using EpicRPGBot.UI.Dungeon;
using Xunit;

namespace EpicRPGBot.Tests.Dungeon
{
    public sealed class CompleteDungeonRunCoordinatorTests
    {
        [Fact]
        public async Task RunAsync_RunsAreaTradeBeforeDungeonPhase()
        {
            var coordinator = new CompleteDungeonRunCoordinator();
            var steps = new List<string>();

            var result = await coordinator.RunAsync(
                (report, cancellationToken) =>
                {
                    steps.Add("trade");
                    return Task.FromResult(CraftJobResult.CompletedResult("Area trade completed."));
                },
                (report, cancellationToken) =>
                {
                    steps.Add("dungeon");
                    return Task.FromResult(DungeonRunResult.CompletedResult("Dungeon completed."));
                },
                _ => { },
                CancellationToken.None);

            Assert.True(result.Completed);
            Assert.Equal(new[] { "trade", "dungeon" }, steps);
        }

        [Fact]
        public async Task RunAsync_StopsWhenAreaTradeFails()
        {
            var coordinator = new CompleteDungeonRunCoordinator();
            var dungeonCalled = false;

            var result = await coordinator.RunAsync(
                (report, cancellationToken) =>
                    Task.FromResult(CraftJobResult.FailedResult("Area trade failed.")),
                (report, cancellationToken) =>
                {
                    dungeonCalled = true;
                    return Task.FromResult(DungeonRunResult.CompletedResult("Dungeon completed."));
                },
                _ => { },
                CancellationToken.None);

            Assert.False(result.Completed);
            Assert.False(dungeonCalled);
            Assert.Contains("pre-dungeon area trade failed", result.Summary);
        }
    }
}
