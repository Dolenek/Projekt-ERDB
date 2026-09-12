using EpicRPGBot.UI.Models;
using EpicRPGBot.UI.Pets;
using Xunit;

namespace EpicRPGBot.Tests.Pets;

public sealed class PetLiveInventoryTests
{
    [Fact]
    public void ParsesActualRenderedFirstPage()
    {
        var page = PetPageParser.Parse(LiveText);
        Assert.NotNull(page);
        Assert.Equal("firendr", page.Owner);
        Assert.Equal(60, page.Total);
        Assert.Equal(10, page.Pages);
        Assert.Equal(6, page.Pets.Count);
        Assert.All(page.Pets, pet => Assert.True(pet.Recognized));
        Assert.Equal("Snowman", page.Pets[4].Species);
        Assert.Equal(6, page.Pets[4].Tier);
    }

    private const string LiveText = @"EPIC RPG
VERIFIED APP
APP
 — 
11:13
Saturday, 12 September 2026 at 11:13
firendr — pets
Pets can collect items and coins, more information with pets info
Total pets: 60/60
In adventure: 0/4
Page: 1/10
ID: A
 Cat — TIER V
Pet score: 305
Status: idle
 EPIC [C]
ID: B
 Pony — TIER V
Pet score: 25
Status: idle
 Farmer
ID: C
 Worker — TIER V
Pet score: 25
Status: idle
 Leader
ID: D
 Cat — TIER IX
Pet score: 225
Status: idle
 Fast [F]
 EPIC [F]
ID: E
 Snowman — TIER VI
Pet score: 36
Status: idle
 Gifter
ID: F
 Dog — TIER VII
Pet score: 49
Status: idle

Pets commands
pets info - information about pets
pets summary - a summary of your pets
pets adventure - a pet will start an adventure, it may find some items for you!
pets claim - claim your pet adventure rewards
pets fusion - select 2 or more pets, there is a chance to get a higher tier pet
pets tournament - send a pet to the pet tournament!
pets release - release some pets if you have too many!
pets ascend - the only way to get the ascended skill
no pet registered for the next pet tournament!
,
,
,
,
,
(edited)
Saturday, 12 September 2026 at 11:16";
}

