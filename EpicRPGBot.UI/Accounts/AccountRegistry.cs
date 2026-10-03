#nullable disable

using System;
using System.IO;
using System.Linq;

namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountRegistry
    {
        private readonly IAccountRegistryStorage _storage;
        private readonly AccountRegistryValidator _validator;
        private AccountRegistryDocument _document;

        public AccountRegistry(string settingsRoot = null) : this(settingsRoot, null) { }

        internal AccountRegistry(string settingsRoot, IAccountRegistryStorage storage)
        {
            SettingsRoot = settingsRoot ?? GetDefaultSettingsRoot();
            _storage = storage ?? new AccountRegistryStorage(SettingsRoot);
            _validator = new AccountRegistryValidator(SettingsRoot);
        }

        public string SettingsRoot { get; }

        public AccountRegistrySnapshot Load()
        {
            var candidate = _storage.Read();
            var isMigration = candidate == null;
            candidate = candidate ?? AccountRegistryMigration.CreateDocument();
            _validator.Validate(candidate);
            var selectionChanged = NormalizeSelection(candidate);
            if (isMigration) AccountRegistryMigration.Save(SettingsRoot, candidate, _storage);
            else if (selectionChanged) _storage.Write(candidate);
            _document = candidate;
            return CreateSnapshot();
        }

        public AccountDefinition Add(string displayName)
        {
            EnsureLoaded();
            var accountId = Guid.NewGuid();
            var record = new AccountRegistryRecord
            {
                AccountId = accountId, DisplayName = displayName,
                SettingsFileName = Path.Combine("accounts", accountId.ToString("N") + ".ini"),
                BrowserProfileName = "account-" + accountId.ToString("N")
            };
            var definition = ToDefinition(record);
            record.DisplayName = definition.DisplayName;
            var candidate = CopyDocument();
            candidate.Accounts.Add(record);
            candidate.SelectedAccountId = accountId;
            Commit(candidate);
            return definition;
        }

        public void Rename(Guid accountId, string displayName)
        {
            EnsureLoaded();
            var candidate = CopyDocument();
            var record = candidate.Accounts.Single(item => item.AccountId == accountId);
            var definition = ToDefinition(record);
            definition.Rename(displayName);
            record.DisplayName = definition.DisplayName;
            Commit(candidate);
        }

        public void Select(Guid accountId)
        {
            EnsureLoaded();
            if (_document.Accounts.All(item => item.AccountId != accountId))
                throw new ArgumentException("Unknown account.", nameof(accountId));
            if (_document.SelectedAccountId == accountId) return;
            var candidate = CopyDocument();
            candidate.SelectedAccountId = accountId;
            Commit(candidate);
        }

        public string ResolveSettingsPath(AccountDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return _validator.ResolveSettingsPath(definition.SettingsFileName);
        }

        private void Commit(AccountRegistryDocument candidate)
        {
            _validator.Validate(candidate);
            _storage.Write(candidate);
            _document = candidate;
        }

        private AccountRegistryDocument CopyDocument()
        {
            return new AccountRegistryDocument
            {
                Version = _document.Version, SelectedAccountId = _document.SelectedAccountId,
                Accounts = _document.Accounts.Select(record => new AccountRegistryRecord
                {
                    AccountId = record.AccountId, DisplayName = record.DisplayName,
                    SettingsFileName = record.SettingsFileName, BrowserProfileName = record.BrowserProfileName
                }).ToList()
            };
        }

        private static bool NormalizeSelection(AccountRegistryDocument document)
        {
            if (document.Accounts.Any(item => item.AccountId == document.SelectedAccountId)) return false;
            document.SelectedAccountId = document.Accounts[0].AccountId;
            return true;
        }

        private AccountRegistrySnapshot CreateSnapshot()
        {
            return new AccountRegistrySnapshot(_document.Accounts.Select(ToDefinition).ToArray(), _document.SelectedAccountId);
        }

        private static AccountDefinition ToDefinition(AccountRegistryRecord record)
        {
            return new AccountDefinition(record.AccountId, record.DisplayName, record.SettingsFileName, record.BrowserProfileName);
        }

        private void EnsureLoaded()
        {
            if (_document == null) Load();
        }

        private static string GetDefaultSettingsRoot()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EpicRPGBot.UI", "settings");
        }
    }
}
