using EpicRPGBot.UI.Pets;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetIdentityTests
{
    [Fact]
    public void FusionRemapsTargetAndLockedSurvivorAfterRenumbering()
    {
        var before = Inventory(Pet("A"), Pet("B"), Pet("C", 5));
        var after = Inventory(Pet("A", 5), Pet("B", 2));
        var request = Request(PetFusionMode.Upgrade, "B");
        request.TargetId = "A";
        request.LockedIds.Add("C");
        var result = PetIdentityMapper.Remap(before, after, request, before.Pets.Take(2).ToArray());
        Assert.Equal("B", result.Id);
        Assert.Equal("B", request.TargetId);
        Assert.Equal(new[] { "A" }, request.LockedIds);
    }

    [Fact]
    public void AmbiguousLockDoesNotFallBackToSameId()
    {
        var before = Inventory(Pet("A"), Pet("B"));
        var request = Request(PetFusionMode.Manual, "B");
        request.LockedIds.Add("A");
        Assert.Throws<InvalidOperationException>(() => PetIdentityMapper.Remap(before, Inventory(Pet("A"), Pet("B")), request));
        Assert.Contains("A", request.LockedIds);
    }

    [Fact]
    public void UnchangedTierIsValidFusionResult()
    {
        var before = Inventory(Pet("A", 2), Pet("B"));
        var result = PetIdentityMapper.Remap(before, Inventory(Pet("Q", 2)), Request(PetFusionMode.Manual, "A", "B"), before.Pets);
        Assert.Equal(2, result.Tier);
    }

    [Fact]
    public void UnexpectedCountOrOwnerStopsReconciliation()
    {
        var before = Inventory(Pet("A"), Pet("B"));
        Assert.Throws<InvalidOperationException>(() => PetIdentityMapper.Remap(before, before,
            Request(PetFusionMode.Manual, "A", "B"), before.Pets));
        var other = Inventory(Pet("A"), Pet("B"));
        other.Owner = "someone else";
        Assert.Throws<InvalidOperationException>(() => PetIdentityMapper.Remap(before, other, new PetFusionRequest()));
    }
}
