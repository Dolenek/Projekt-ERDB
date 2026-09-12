using EpicRPGBot.UI.Pets;
using EpicRPGBot.UI.Services;
using Xunit;
using static EpicRPGBot.Tests.Pets.PetTestFactory;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetSettingsTests
{
    [Fact]
    public void SettingsAndLocksRoundTripByFingerprintNotId()
    {
        var path = Path.Combine(Path.GetTempPath(), "pet-settings-" + Guid.NewGuid() + ".ini");
        try
        {
            var store = new PetSettingsStore(new LocalSettingsStore(path));
            store.Save(new PetOptions { TimeTravel = 121, KeepBest = false, MixSpecies = true });
            store.SaveLocks(Inventory(Pet("A", 5)), new HashSet<string> { "A" });
            var loaded = new PetSettingsStore(new LocalSettingsStore(path));
            Assert.Equal(121, loaded.Load().TimeTravel);
            Assert.False(loaded.Load().KeepBest);
            Assert.True(loaded.Load().MixSpecies);
            Assert.Equal(new[] { "Z" }, loaded.LoadLocks(Inventory(Pet("Z", 5))));
            Assert.Throws<InvalidOperationException>(() => loaded.LoadLocks(Inventory(Pet("X", 5), Pet("Y", 5))));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("Mystery", "Fast [F]")]
    [InlineData("Cat", "Fast [X]")]
    [InlineData("Cat", "New Skill [SS+]")]
    [InlineData("Cat", "Fast")]
    [InlineData("Cat", "新技能")]
    public void UnknownSpeciesOrSkillRemainsIneligible(string species, string skills)
    {
        var page = PetPageParser.Parse(Page(Entry("A", species: species, skills: skills), 1));
        Assert.False(page.Pets[0].Recognized);
    }

    [Fact]
    public void CustomEmojiLabelsAreNotTreatedAsSkills()
    {
        var page = PetPageParser.Parse(Page(Entry("A", skills: ":epic: EPIC [C]\n:fast: Fast [SS+]"), 1));
        Assert.True(page.Pets[0].Recognized);
        Assert.Equal("EPIC [C], Fast [SS+]", page.Pets[0].Skills);
    }
}
