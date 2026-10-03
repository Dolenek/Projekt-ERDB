using System.Text.Json;
using EpicRPGBot.UI.Accounts;
using Xunit;

namespace EpicRPGBot.Tests.Accounts;

public sealed class AccountRegistryValidationTests : IDisposable
{
    private readonly string _settingsRoot = Path.Combine(Path.GetTempPath(), "EpicRPGBot.Tests", Guid.NewGuid().ToString("N"));
    private string RegistryPath => Path.Combine(_settingsRoot, "accounts.json");

    [Theory]
    [InlineData("{unfinished")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("")]
    public void InvalidExistingRegistryIsPreserved(string original)
    {
        Directory.CreateDirectory(_settingsRoot);
        File.WriteAllText(RegistryPath, original);

        Assert.Throws<InvalidDataException>(() => new AccountRegistry(_settingsRoot).Load());

        Assert.Equal(original, File.ReadAllText(RegistryPath));
        Assert.False(Directory.Exists(Path.Combine(_settingsRoot, "accounts")));
    }

    [Theory]
    [InlineData("account-id")]
    [InlineData("settings-file")]
    [InlineData("profile")]
    [InlineData("outside-root")]
    [InlineData("rooted-path")]
    [InlineData("version")]
    [InlineData("missing-name")]
    [InlineData("null-record")]
    [InlineData("empty-accounts")]
    public void InvalidOrAmbiguousAccountsAreRejectedBeforeWriting(string invalidField)
    {
        var document = CreateValidDocument();
        InvalidateDocument(document, invalidField);
        Directory.CreateDirectory(_settingsRoot);
        var original = JsonSerializer.Serialize(document);
        File.WriteAllText(RegistryPath, original);

        Assert.Throws<InvalidDataException>(() => new AccountRegistry(_settingsRoot).Load());

        Assert.Equal(original, File.ReadAllText(RegistryPath));
    }

    [Fact]
    public void MissingSelectionUsesFirstAccountWithoutChangingIdentities()
    {
        var document = CreateValidDocument();
        document.SelectedAccountId = Guid.NewGuid();
        Directory.CreateDirectory(_settingsRoot);
        File.WriteAllText(RegistryPath, JsonSerializer.Serialize(document));

        var snapshot = new AccountRegistry(_settingsRoot).Load();

        Assert.Equal(document.Accounts[0].AccountId, snapshot.SelectedAccountId);
        Assert.Equal(document.Accounts.Select(account => account.AccountId), snapshot.Accounts.Select(account => account.AccountId));
    }

    private void InvalidateDocument(AccountRegistryDocument document, string invalidField)
    {
        var first = document.Accounts[0];
        var second = document.Accounts[1];
        switch (invalidField)
        {
            case "account-id": second.AccountId = first.AccountId; break;
            case "settings-file": second.SettingsFileName = "accounts/../ACCOUNTS/FIRST.ini"; break;
            case "profile": second.BrowserProfileName = first.BrowserProfileName.ToUpperInvariant(); break;
            case "outside-root": second.SettingsFileName = "../outside.ini"; break;
            case "rooted-path": second.SettingsFileName = Path.Combine(_settingsRoot, "inside.ini"); break;
            case "version": document.Version = 2; break;
            case "missing-name": second.DisplayName = " "; break;
            case "null-record": document.Accounts[1] = null!; break;
            case "empty-accounts": document.Accounts.Clear(); break;
        }
    }

    private static AccountRegistryDocument CreateValidDocument()
    {
        var first = CreateRecord("First");
        return new AccountRegistryDocument
        {
            Version = 1, SelectedAccountId = first.AccountId,
            Accounts = new List<AccountRegistryRecord> { first, CreateRecord("Second") }
        };
    }

    private static AccountRegistryRecord CreateRecord(string name) => new()
    {
        AccountId = Guid.NewGuid(), DisplayName = name,
        SettingsFileName = "accounts/" + name.ToLowerInvariant() + ".ini", BrowserProfileName = "account-" + name
    };

    public void Dispose()
    {
        if (Directory.Exists(_settingsRoot)) Directory.Delete(_settingsRoot, true);
    }
}
