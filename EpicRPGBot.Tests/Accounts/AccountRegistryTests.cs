using EpicRPGBot.UI.Accounts;
using EpicRPGBot.UI.Services;
using Xunit;

namespace EpicRPGBot.Tests.Accounts;

public sealed class AccountRegistryTests : IDisposable
{
    private readonly string _settingsRoot = Path.Combine(
        Path.GetTempPath(), "EpicRPGBot.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void FirstLoadMigratesLegacySettingsAndProfile()
    {
        Directory.CreateDirectory(_settingsRoot);
        File.WriteAllText(Path.Combine(_settingsRoot, "app-settings.ini"), "area=15");

        var snapshot = new AccountRegistry(_settingsRoot).Load();

        var account = Assert.Single(snapshot.Accounts);
        Assert.Contains(account.AccountId.ToString("N"), account.SettingsFileName);
        Assert.Equal("Default", account.BrowserProfileName);
        Assert.Equal(account.AccountId, snapshot.SelectedAccountId);
        Assert.Equal("area=15", File.ReadAllText(Path.Combine(_settingsRoot, "app-settings.ini")));
        Assert.Equal("area=15", File.ReadAllText(ResolveAccountSettingsPath(account)));
    }

    [Fact]
    public void AddedAccountsReceiveStableSeparateStorageAndProfiles()
    {
        var registry = new AccountRegistry(_settingsRoot);
        registry.Load();

        var second = registry.Add("Second");
        var reloaded = new AccountRegistry(_settingsRoot).Load();

        Assert.Equal(2, reloaded.Accounts.Count);
        Assert.Equal(second.AccountId, reloaded.SelectedAccountId);
        Assert.Contains(second.AccountId.ToString("N"), second.SettingsFileName);
        Assert.Contains(second.AccountId.ToString("N"), second.BrowserProfileName);
        Assert.NotEqual(reloaded.Accounts[0].SettingsFileName, second.SettingsFileName);
        Assert.NotEqual(reloaded.Accounts[0].BrowserProfileName, second.BrowserProfileName);
    }

    [Fact]
    public void AccountSettingsStoresAreIsolated()
    {
        var registry = new AccountRegistry(_settingsRoot);
        var first = registry.Load().Accounts.Single();
        var second = registry.Add("Second");
        var firstStore = new LocalSettingsStore(registry.ResolveSettingsPath(first), true);
        var secondStore = new LocalSettingsStore(registry.ResolveSettingsPath(second), true);

        firstStore.SetString("channel_url", "https://discord.test/first");
        secondStore.SetString("channel_url", "https://discord.test/second");

        Assert.Equal("https://discord.test/first", firstStore.GetString("channel_url"));
        Assert.Equal("https://discord.test/second", secondStore.GetString("channel_url"));
    }

    [Fact]
    public void RepeatedMigrationKeepsTheSameAccountIdentity()
    {
        var firstLoad = new AccountRegistry(_settingsRoot).Load();
        var secondLoad = new AccountRegistry(_settingsRoot).Load();

        Assert.Equal(firstLoad.SelectedAccountId, secondLoad.SelectedAccountId);
        Assert.Equal(firstLoad.Accounts.Single().BrowserProfileName,
            secondLoad.Accounts.Single().BrowserProfileName);
    }

    [Fact]
    public void RenameDoesNotChangeAccountIdentityOrStorage()
    {
        var registry = new AccountRegistry(_settingsRoot);
        registry.Load();
        var account = registry.Add("Before");

        registry.Rename(account.AccountId, "After");
        var renamed = new AccountRegistry(_settingsRoot).Load().Accounts
            .Single(item => item.AccountId == account.AccountId);

        Assert.Equal("After", renamed.DisplayName);
        Assert.Equal(account.SettingsFileName, renamed.SettingsFileName);
        Assert.Equal(account.BrowserProfileName, renamed.BrowserProfileName);
    }

    public void Dispose()
    {
        if (Directory.Exists(_settingsRoot)) Directory.Delete(_settingsRoot, true);
    }

    private string ResolveAccountSettingsPath(AccountDefinition account)
    {
        return Path.Combine(_settingsRoot, account.SettingsFileName);
    }
}
