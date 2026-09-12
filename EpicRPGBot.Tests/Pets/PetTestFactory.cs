using EpicRPGBot.UI.Pets;

namespace EpicRPGBot.Tests.Pets;

internal static class PetTestFactory
{
    internal static PetRecord Pet(string id, int tier = 1, string species = "Cat", int score = 0, string skills = "") =>
        new() { Id = id, Tier = tier, Species = species, Score = score, Skills = skills, Status = "idle", Recognized = true };

    internal static PetInventory Inventory(params PetRecord[] pets) => new() { Owner = "friendr", Complete = true, Pets = pets };

    internal static PetFusionRequest Request(PetFusionMode mode, params string[] ids) => new() {
        Mode = mode, MaterialIds = new HashSet<string>(ids), Goal = 2,
        Options = new PetOptions { TimeTravel = 0, KeepBest = false, ProtectSpecial = false }
    };

    internal static string Page(string entries, int total = 2, int page = 1, int pages = 1) =>
        $"friendr — pets\nTotal pets: {total}/60\nIn adventure: 0/4\nPage: {page}/{pages}\n{entries}\nPets commands\npets fusion";

    internal static string Entry(string id, string tier = "I", string species = "Cat", string skills = "") =>
        $"ID: {id}\n🐱 {species} — TIER {tier}\nPet score: 25\nStatus: idle\n{skills}\n";
}
