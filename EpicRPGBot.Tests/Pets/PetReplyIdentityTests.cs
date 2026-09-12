using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Pets;
using Xunit;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetReplyIdentityTests
{
    [Theory]
    [InlineData("EPIC RPG")]
    [InlineData("EPIC RPG APP")]
    [InlineData("EPIC RPGAPP")]
    [InlineData("EPIC RPGVerified AppAPP")]
    [InlineData("EPIC RPG ✓APP")]
    [InlineData("EPIC\u00a0RPG")]
    [InlineData("\u200bEPIC RPG\u200e")]
    public void AcceptsDiscordNameDecorations(string author) =>
        Assert.True(PetReplyIdentity.IsEpic(new DiscordMessageSnapshot("1", "pets", author)));

    [Fact]
    public void MissingMetadataCanUseLeadingDiscordHeader() =>
        Assert.True(PetReplyIdentity.IsEpic(new DiscordMessageSnapshot("1", "EPIC RPG\nAPP\n11:02\nfirendr — pets", "")));

    [Theory]
    [InlineData("someone else", "EPIC RPG\nfirendr — pets")]
    [InlineData("", "firendr — pets\nSome text mentioning EPIC RPG")]
    [InlineData("Fake EPIC RPG", "pets")]
    public void RejectsOtherAuthorsAndBodyMentions(string author, string text) =>
        Assert.False(PetReplyIdentity.IsEpic(new DiscordMessageSnapshot("1", text, author)));
}
