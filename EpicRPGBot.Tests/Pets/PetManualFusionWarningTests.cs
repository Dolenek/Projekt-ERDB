using EpicRPGBot.UI.Pets;
using Xunit;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetManualFusionWarningTests
{
    [Theory]
    [InlineData(PetFusionMode.Manual, 2, false)]
    [InlineData(PetFusionMode.Manual, 3, false)]
    [InlineData(PetFusionMode.Manual, 4, true)]
    [InlineData(PetFusionMode.Manual, 48, true)]
    [InlineData(PetFusionMode.Upgrade, 48, false)]
    [InlineData(PetFusionMode.Reduce, 48, false)]
    public void WarnsOnlyForManualGroupsLargerThanThree(PetFusionMode mode, int count, bool expected)
    {
        var request = new PetFusionRequest { Mode = mode, MaterialIds = new HashSet<string>(Enumerable.Range(1, count).Select(i => i.ToString())) };
        Assert.Equal(expected, PetManualFusionWarning.Required(request));
    }

    [Fact]
    public void WarningIdentifiesGroupAndIrreversibleSingleFusion()
    {
        var request = new PetFusionRequest { MaterialIds = new HashSet<string> { "D", "B", "A", "C" } };
        var warning = PetManualFusionWarning.Describe(request);
        Assert.Contains("4 pets", warning);
        Assert.Contains("ONE fusion", warning);
        Assert.Contains("cannot be undone", warning);
        Assert.Contains("A B C D", warning);
    }

    [Theory]
    [InlineData("firendr", true)]
    [InlineData("other player", false)]
    public void RecognizesActualSuccessfulFusionOnlyForOwner(string owner, bool expected) =>
        Assert.Equal(expected, PetFusionReplyParser.IsSuccess(
            "EPIC RPG\nVERIFIED APP\nAPP\nfirendr — pets\nYou have got a new pet!\nID: J\nCat — TIER VI\nFast [F]\nHappy [F]", owner));
}
