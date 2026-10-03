using EpicRPGBot.UI.Accounts;
using EpicRPGBot.Tests.Puzzle;
using Xunit;

namespace EpicRPGBot.Tests.Accounts;

public sealed class AccountRegistryPersistenceTests : IDisposable
{
    private readonly string _settingsRoot = Path.Combine(Path.GetTempPath(), "EpicRPGBot.Tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void FailedAddCannotLeakIntoNextSave()
    {
        var storage = new FailingRegistryStorage(_settingsRoot);
        var registry = new AccountRegistry(_settingsRoot, storage);
        var first = registry.Load().Accounts.Single();
        var original = File.ReadAllText(RegistryPath);
        storage.RejectWrites = true;

        Assert.Throws<IOException>(() => registry.Add("Uncommitted"));
        Assert.Equal(original, File.ReadAllText(RegistryPath));
        storage.RejectWrites = false;
        registry.Rename(first.AccountId, "Kept");

        Assert.Equal("Kept", new AccountRegistry(_settingsRoot).Load().Accounts.Single().DisplayName);
    }

    [Fact]
    public void FailedRenameCannotLeakIntoNextSave()
    {
        var storage = new FailingRegistryStorage(_settingsRoot);
        var registry = new AccountRegistry(_settingsRoot, storage);
        var first = registry.Load().Accounts.Single();
        registry.Add("Second");
        var original = File.ReadAllText(RegistryPath);
        storage.RejectWrites = true;

        Assert.Throws<IOException>(() => registry.Rename(first.AccountId, "Uncommitted"));
        Assert.Equal(original, File.ReadAllText(RegistryPath));
        storage.RejectWrites = false;
        registry.Select(first.AccountId);

        Assert.Equal(first.DisplayName, new AccountRegistry(_settingsRoot).Load().Accounts[0].DisplayName);
    }

    [Fact]
    public void FailedSelectionKeepsPreviouslySelectedAccount()
    {
        var storage = new FailingRegistryStorage(_settingsRoot);
        var registry = new AccountRegistry(_settingsRoot, storage);
        var first = registry.Load().Accounts.Single();
        var second = registry.Add("Second");
        storage.RejectWrites = true;

        Assert.Throws<IOException>(() => registry.Select(first.AccountId));
        registry.Select(second.AccountId);
        storage.RejectWrites = false;
        registry.Rename(first.AccountId, "First");

        Assert.Equal(second.AccountId, new AccountRegistry(_settingsRoot).Load().SelectedAccountId);
    }

    [Fact]
    public void FailedMigrationPreservesLegacySettingsAndRemovesOnlyItsNewCopy()
    {
        Directory.CreateDirectory(_settingsRoot);
        var legacyPath = Path.Combine(_settingsRoot, "app-settings.ini");
        File.WriteAllText(legacyPath, "area=15");
        var storage = new FailingRegistryStorage(_settingsRoot) { RejectWrites = true };
        var registry = new AccountRegistry(_settingsRoot, storage);

        Assert.Throws<IOException>(() => registry.Load());

        Assert.Equal("area=15", File.ReadAllText(legacyPath));
        Assert.Empty(Directory.GetFiles(Path.Combine(_settingsRoot, "accounts")));
        Assert.False(File.Exists(RegistryPath));
        storage.RejectWrites = false;
        var migrated = registry.Load();
        Assert.Equal(migrated.SelectedAccountId, new AccountRegistry(_settingsRoot).Load().SelectedAccountId);
    }

    [WindowsFact]
    public void LockedRegistryWritePreservesOriginalAndCleansStagedFile()
    {
        var registry = new AccountRegistry(_settingsRoot);
        var first = registry.Load().Accounts.Single();
        var original = File.ReadAllText(RegistryPath);
        using (var locked = new FileStream(RegistryPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert.ThrowsAny<IOException>(() => registry.Rename(first.AccountId, "Uncommitted"));

        Assert.Equal(original, File.ReadAllText(RegistryPath));
        Assert.Empty(Directory.GetFiles(_settingsRoot, "*.tmp"));
        registry.Add("Second");
        Assert.Equal(first.DisplayName, new AccountRegistry(_settingsRoot).Load().Accounts[0].DisplayName);
    }

    [Fact]
    public void UnreadableRegistryDoesNotStartMigration()
    {
        Directory.CreateDirectory(RegistryPath);

        Assert.ThrowsAny<UnauthorizedAccessException>(() => new AccountRegistry(_settingsRoot).Load());

        Assert.False(Directory.Exists(Path.Combine(_settingsRoot, "accounts")));
    }

    private string RegistryPath => Path.Combine(_settingsRoot, "accounts.json");

    public void Dispose()
    {
        if (Directory.Exists(_settingsRoot)) Directory.Delete(_settingsRoot, true);
    }

    private sealed class FailingRegistryStorage : IAccountRegistryStorage
    {
        private readonly AccountRegistryStorage _storage;
        public FailingRegistryStorage(string root) => _storage = new AccountRegistryStorage(root);
        public bool RejectWrites { get; set; }
        public AccountRegistryDocument Read() => _storage.Read();
        public void Write(AccountRegistryDocument document)
        {
            if (RejectWrites) throw new IOException("Simulated registry write failure.");
            _storage.Write(document);
        }
    }
}
