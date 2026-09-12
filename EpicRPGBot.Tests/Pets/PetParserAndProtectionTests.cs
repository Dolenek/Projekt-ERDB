using EpicRPGBot.UI.Pets;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetParserAndProtectionTests
{
    [Fact]
    public void ParsesRenderedInventoryWithRankedAndSpecialSkills()
    {
        var page = PetPageParser.Parse(Page(Entry("A", "XXV", skills: "Fast [SS+]\nEPIC [C]") +
            Entry("B", "VI", "Snowman", "Gifter")));
        Assert.Equal(25, page.Pets[0].Tier);
        Assert.True(page.Pets[0].HasSkill("EPIC"));
        Assert.Contains("Fast [SS+]", page.Pets[0].Skills);
        Assert.Equal("Gifter", page.Pets[1].Skills);
        Assert.True(page.Pets[1].IsSpecial);
        Assert.All(page.Pets, pet => Assert.True(pet.Recognized));
    }

    [Theory]
    [InlineData("I", 1)] [InlineData("IX", 9)] [InlineData("XXV", 25)]
    [InlineData("IIII", 0)] [InlineData("XXVI", 0)]
    public void RomanTiersAreValidated(string roman, int expected) => Assert.Equal(expected, PetPageParser.RomanTier(roman));

    [Fact]
    public void MissingScoreLeavesPetVisibleButIneligible()
    {
        var page = PetPageParser.Parse(Page(Entry("A").Replace("Pet score: 25", ""), 1));
        Assert.Single(page.Pets);
        Assert.False(page.Pets[0].Recognized);
    }

    [Fact]
    public void BestProtectionUsesTierThenScoreThenId()
    {
        var inventory = Inventory(Pet("B", 5, score: 50), Pet("A", 5, score: 50), Pet("C", 4, score: 100));
        var request = new PetFusionRequest();
        Assert.Equal("Best of species protected", PetProtection.Reason(inventory.Pets[1], inventory, request));
        Assert.Equal("", PetProtection.Reason(inventory.Pets[0], inventory, request));
    }

    [Theory]
    [InlineData("EPIC [F]")] [InlineData("ASCENDED [SS+]")] [InlineData("PERFECT [S]")]
    public void RareSkillsProtectedByDefault(string skills)
    {
        var pet = Pet("A", skills: skills);
        Assert.Contains("protected", PetProtection.Reason(pet, Inventory(pet), new PetFusionRequest()));
    }

    [Fact]
    public void SpeciesUsesMajorityAndSpecialPriority()
    {
        Assert.Equal(new[] { "Cat" }, PetProtection.ResultSpecies(new[] { Pet("A"), Pet("B"), Pet("C", species: "Dragon") }));
        Assert.Equal(new[] { "Bunny" }, PetProtection.ResultSpecies(new[] { Pet("A"), Pet("B"), Pet("C", species: "Bunny") }));
        Assert.False(PetProtection.Compatible(new[] { Pet("A", species: "Bunny"), Pet("B", species: "Snowman") }));
        Assert.False(PetProtection.Compatible(new[] { Pet("A", skills: "Farmer"), Pet("B", skills: "Leader") }));
    }
}
