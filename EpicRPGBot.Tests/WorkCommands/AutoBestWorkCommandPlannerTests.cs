using System.Collections.Generic;
using EpicRPGBot.UI.WorkCommands;
using Xunit;

namespace EpicRPGBot.Tests.WorkCommands;

public sealed class AutoBestWorkCommandPlannerTests
{
    [Fact]
    public void RegularPlan_BelowTwentyMillion_PrioritizesCoins()
    {
        var plan = BuildRegular(19999999m, 37);

        AssertAreas(plan, "chop", 1, 2);
        AssertAreas(plan, "axe", 3, 4, 5);
        AssertAreas(plan, "pickaxe", 6, 7, 8, 9);
        AssertAreas(plan, "drill", 10, 11);
        AssertAreas(plan, "dynamite", 12, 13, 14, 15);
    }

    [Fact]
    public void RegularPlan_AtTwentyMillionWithTwoTravels_UsesInclusiveTravelRule()
    {
        var plan = BuildRegular(20000000m, 2);

        AssertAreas(plan, "pickaxe", 6, 7, 8);
        AssertAreas(plan, "chainsaw", 9);
        AssertAreas(plan, "drill", 10, 11);
        AssertAreas(plan, "dynamite", 12, 13, 14, 15);
    }

    [Fact]
    public void RegularPlan_AtTwentyMillionWithThreeTravels_UsesMaterialCommands()
    {
        var plan = BuildRegular(20000000m, 3);

        AssertAreas(plan, "ladder", 6, 7);
        AssertAreas(plan, "bowsaw", 8);
        AssertAreas(plan, "chainsaw", 9);
        AssertAreas(plan, "drill", 10, 11);
        AssertAreas(plan, "dynamite", 12, 13, 14, 15);
    }

    [Fact]
    public void RegularPlan_AtThirtyMillion_UsesChainsawFromAreaNine()
    {
        var plan = BuildRegular(30000000m, 3);

        AssertAreas(plan, "ladder", 6, 7);
        AssertAreas(plan, "bowsaw", 8);
        AssertAreas(plan, "chainsaw", 9, 10, 11, 12, 13, 14, 15);
    }

    [Theory]
    [MemberData(nameof(AscendedBands))]
    public void AscendedPlan_UsesExactWorkerBand(
        bool bothPotions,
        int workerLevel,
        string expectedGroups)
    {
        var plan = AutoBestWorkCommandPlanner.Build(
            0,
            0,
            true,
            workerLevel,
            bothPotions,
            bothPotions);

        Assert.Equal(
            bothPotions
                ? AutoBestWorkCommandMode.AscendedWithPotions
                : AutoBestWorkCommandMode.AscendedWithoutPotions,
            plan.Mode);
        AssertGroupCommands(plan.Selections, expectedGroups.Split(','));
    }

    [Fact]
    public void AscendedPlan_WithOnlyOnePotion_UsesNoPotionsTable()
    {
        var plan = AutoBestWorkCommandPlanner.Build(0, 0, true, 104, true, false);

        Assert.Equal(AutoBestWorkCommandMode.AscendedWithoutPotions, plan.Mode);
        AssertAreas(plan.Selections, "greenhouse", 6, 7);
        AssertAreas(plan.Selections, "dynamite", 8, 9);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(99)]
    public void AscendedPlan_WithoutEligibleWorker_FallsBackToRegular(int? workerLevel)
    {
        var plan = AutoBestWorkCommandPlanner.Build(30000000m, 3, true, workerLevel, true, true);

        Assert.Equal(AutoBestWorkCommandMode.Regular, plan.Mode);
        AssertAreas(plan.Selections, "ladder", 6, 7);
    }

    public static IEnumerable<object[]> AscendedBands()
    {
        const string first = "dynamite,dynamite,dynamite,dynamite,dynamite,chainsaw";
        const string second = "dynamite,dynamite,greenhouse,dynamite,dynamite,chainsaw";
        const string third = "dynamite,dynamite,greenhouse,dynamite,greenhouse,chainsaw";
        const string fourth = "dynamite,dynamite,greenhouse,chainsaw,greenhouse,chainsaw";
        const string fifth = "dynamite,chainsaw,greenhouse,chainsaw,greenhouse,chainsaw";

        foreach (var row in Band(false, first, 100, 101)) yield return row;
        foreach (var row in Band(false, second, 102, 106)) yield return row;
        foreach (var row in Band(false, third, 107, 108)) yield return row;
        foreach (var row in Band(false, fourth, 109, 114)) yield return row;
        foreach (var row in Band(false, fifth, 115, 200)) yield return row;
        foreach (var row in Band(true, first, 100, 108)) yield return row;
        foreach (var row in Band(true, second, 109, 113)) yield return row;
        foreach (var row in Band(true, third, 114, 117)) yield return row;
        foreach (var row in Band(true, fourth, 118, 122)) yield return row;
        foreach (var row in Band(true, fifth, 123, 200)) yield return row;
    }

    private static IEnumerable<object[]> Band(bool both, string commands, int minimum, int maximum)
    {
        yield return new object[] { both, minimum, commands };
        if (maximum != minimum) yield return new object[] { both, maximum, commands };
    }

    private static IReadOnlyDictionary<int, string> BuildRegular(decimal coins, int travels)
    {
        return AutoBestWorkCommandPlanner.Build(coins, travels, false, null, false, false).Selections;
    }

    private static void AssertGroupCommands(
        IReadOnlyDictionary<int, string> selections,
        IReadOnlyList<string> commands)
    {
        AssertAreas(selections, commands[0], 1, 2, 3);
        AssertAreas(selections, commands[1], 4, 5);
        AssertAreas(selections, commands[2], 6, 7);
        AssertAreas(selections, commands[3], 8);
        AssertAreas(selections, commands[4], 9);
        AssertAreas(selections, commands[5], 10, 11, 12, 13, 14, 15);
    }

    private static void AssertAreas(
        IReadOnlyDictionary<int, string> selections,
        string command,
        params int[] areas)
    {
        foreach (var area in areas)
        {
            Assert.Equal("rpg " + command, selections[area]);
        }
    }
}
