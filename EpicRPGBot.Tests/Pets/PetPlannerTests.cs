using EpicRPGBot.UI.Pets;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetPlannerTests
{
    private readonly PetFusionPlanner _planner = new();

    [Theory]
    [InlineData(9, 4, 3)] [InlineData(10, 4, 2)] [InlineData(24, 4, 2)] [InlineData(25, 4, 1)]
    [InlineData(40, 6, 3)] [InlineData(41, 6, 2)] [InlineData(60, 6, 2)] [InlineData(61, 6, 1)]
    [InlineData(90, 8, 5)] [InlineData(91, 8, 4)] [InlineData(120, 8, 4)] [InlineData(121, 8, 3)]
    [InlineData(0, 11, 0)] [InlineData(121, 21, 0)]
    public void CatalogHonorsTtBoundaries(int tt, int result, int lower) => Assert.Equal(lower, PetFusionRecipes.LowerParent(result, tt));

    [Fact]
    public void ProtectedTargetCannotBeOverriddenByExplicitSelection()
    {
        var request = Request(PetFusionMode.Upgrade, "B");
        request.TargetId = "A";
        request.Options.KeepBest = true;
        Assert.False(_planner.Next(Inventory(Pet("A"), Pet("B")), request).Available);
    }

    [Fact]
    public void UpgradePreparesMissingMaterialWithoutConsumingTarget()
    {
        var request = Request(PetFusionMode.Upgrade, "B", "C", "D");
        request.TargetId = "A";
        request.Goal = 4;
        var step = _planner.Next(Inventory(Pet("A", 3), Pet("B"), Pet("C"), Pet("D")), request);
        Assert.True(step.Available);
        Assert.Equal(2, step.Parents.Count);
        Assert.DoesNotContain(step.Parents, p => p.Id == "A");
        Assert.All(step.Parents, p => Assert.Equal(1, p.Tier));
    }

    [Fact]
    public void UpgradeUsesReadyMaterialBeforePreparation()
    {
        var request = Request(PetFusionMode.Upgrade, "B", "C", "D", "E");
        request.TargetId = "A";
        request.Goal = 4;
        var step = _planner.Next(Inventory(Pet("A", 3), Pet("B"), Pet("C"), Pet("D"), Pet("E", 3)), request);
        Assert.Equal(new[] { "A", "E" }, step.Parents.Select(p => p.Id));
    }

    [Fact]
    public void MixedOrdinaryPairCannotChangeTargetSpecies()
    {
        var request = Request(PetFusionMode.Upgrade, "B");
        request.TargetId = "A";
        request.Options.MixSpecies = true;
        Assert.False(_planner.Next(Inventory(Pet("A"), Pet("B", species: "Dog")), request).Available);
    }

    [Fact]
    public void SpecialTargetCanUseOrdinaryMaterialWhenProtectionsDisabled()
    {
        var request = Request(PetFusionMode.Upgrade, "B");
        request.TargetId = "A";
        request.Options.MixSpecies = true;
        Assert.True(_planner.Next(Inventory(Pet("A", species: "Bunny"), Pet("B")), request).Available);
    }

    [Fact]
    public void ReductionDoesNotUseUnrecommendedPair()
    {
        var request = Request(PetFusionMode.Reduce, "A", "B", "C");
        var step = _planner.Next(Inventory(Pet("A", 8), Pet("B", 2), Pet("C", 5)), request);
        Assert.False(step.Available);
    }

    [Fact]
    public void ReductionPrefersLowestTierThenLowestScore()
    {
        var request = Request(PetFusionMode.Reduce, "A", "B", "C", "D");
        var step = _planner.Next(Inventory(Pet("A", 2), Pet("B", score: 30), Pet("C", score: 20), Pet("D", score: 10)), request);
        Assert.Equal(new[] { "D", "C" }, step.Parents.Select(p => p.Id));
    }

    [Fact]
    public void ReductionCanPreserveLowerTierSpecialSpecies()
    {
        var request = Request(PetFusionMode.Reduce, "A", "B");
        request.Goal = 1;
        request.Options.MixSpecies = true;
        var step = _planner.Next(Inventory(Pet("A", 2), Pet("B", species: "Bunny")), request);
        Assert.True(step.Available);
        Assert.Equal(new[] { "Bunny" }, PetProtection.ResultSpecies(step.Parents));
    }

    [Fact]
    public void ManualAllowsThreeParentsButRejectsLockedParent()
    {
        var request = Request(PetFusionMode.Manual, "A", "B", "C");
        var inventory = Inventory(Pet("A"), Pet("B"), Pet("C"));
        Assert.Equal(3, _planner.Next(inventory, request).Parents.Count);
        request.LockedIds.Add("C");
        Assert.False(_planner.Next(inventory, request).Available);
    }
}
