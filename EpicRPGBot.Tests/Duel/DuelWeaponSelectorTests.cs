using EpicRPGBot.UI.Duel;
using Xunit;

namespace EpicRPGBot.Tests.Duel;

public sealed class DuelWeaponSelectorTests
{
    private readonly DuelWeaponSelector _selector = new();

    [Fact]
    public void CreditCard_IsPreferredOverEarlierWeapon()
    {
        var prompt = DuelTestMessages.Message(
            "1",
            "choose the weapon",
            buttons: new[] { DuelTestMessages.Button("🗡", 0), DuelTestMessages.Button(":credit_card:", 1) });

        Assert.Equal(":credit_card:", _selector.Select(prompt)?.Label);
    }

    [Fact]
    public void FirstWeapon_IsUsedWhenCreditCardIsAbsent()
    {
        var prompt = DuelTestMessages.Message(
            "1",
            "choose the weapon",
            buttons: new[] { DuelTestMessages.Button("📏", 0), DuelTestMessages.Button("⚓", 1) });

        Assert.Equal("📏", _selector.Select(prompt)?.Label);
    }
}
