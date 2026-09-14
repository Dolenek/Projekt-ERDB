#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountRegistry
    {
        private const string LegacySettingsFile = "app-settings.ini";
        private const string LegacyProfileName = "Default";
        private readonly string _registryPath;
        private AccountRegistryDocument _document;

        public AccountRegistry(string settingsRoot = null)
        {
            SettingsRoot = settingsRoot ?? GetDefaultSettingsRoot();
            _registryPath = Path.Combine(SettingsRoot, "accounts.json");
        }

        public string SettingsRoot { get; }

        public AccountRegistrySnapshot Load()
        {
            _document = ReadDocument() ?? CreateMigratedDocument();
            NormalizeDocument(_document);
            SaveDocument();
            return CreateSnapshot();
        }

        public AccountDefinition Add(string displayName)
        {
            EnsureLoaded();
            var accountId = Guid.NewGuid();
            var record = new AccountRegistryRecord
            {
                AccountId = accountId,
                DisplayName = displayName?.Trim(),
                SettingsFileName = Path.Combine("accounts", accountId.ToString("N") + ".ini"),
                BrowserProfileName = "account-" + accountId.ToString("N")
            };
            var definition = ToDefinition(record);
            _document.Accounts.Add(record);
            _document.SelectedAccountId = accountId;
            SaveDocument();
            return definition;
        }

        public void Rename(Guid accountId, string displayName)
        {
            EnsureLoaded();
            var definition = new AccountDefinition(accountId, displayName, "unused", "unused");
            var record = _document.Accounts.Single(item => item.AccountId == accountId);
            record.DisplayName = definition.DisplayName;
            SaveDocument();
        }

        public void Select(Guid accountId)
        {
            EnsureLoaded();
            if (_document.Accounts.All(item => item.AccountId != accountId))
                throw new ArgumentException("Unknown account.", nameof(accountId));
            _document.SelectedAccountId = accountId;
            SaveDocument();
        }

        public string ResolveSettingsPath(AccountDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            var root = Path.GetFullPath(SettingsRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;
            var candidate = Path.GetFullPath(Path.Combine(root, definition.SettingsFileName));
            if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Account settings path leaves the settings directory.");
            return candidate;
        }

        private AccountRegistryDocument ReadDocument()
        {
            try
            {
                return File.Exists(_registryPath)
                    ? JsonSerializer.Deserialize<AccountRegistryDocument>(File.ReadAllText(_registryPath))
                    : null;
            }
            catch
            {
                return null;
            }
        }

        private AccountRegistryDocument CreateMigratedDocument()
        {
            var accountId = Guid.NewGuid();
            var accountSettingsFile = Path.Combine("accounts", accountId.ToString("N") + ".ini");
            CopyLegacySettings(accountSettingsFile);
            return new AccountRegistryDocument
            {
                Version = 1,
                SelectedAccountId = accountId,
                Accounts = new List<AccountRegistryRecord>
                {
                    new AccountRegistryRecord
                    {
                        AccountId = accountId,
                        DisplayName = "Default",
                        SettingsFileName = accountSettingsFile,
                        BrowserProfileName = LegacyProfileName
                    }
                }
            };
        }

        private void CopyLegacySettings(string accountSettingsFile)
        {
            var sourcePath = Path.Combine(SettingsRoot, LegacySettingsFile);
            var destinationPath = Path.Combine(SettingsRoot, accountSettingsFile);
            if (!File.Exists(sourcePath) || File.Exists(destinationPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath, false);
        }

        private void NormalizeDocument(AccountRegistryDocument document)
        {
            document.Version = 1;
            document.Accounts = document.Accounts ?? new List<AccountRegistryRecord>();
            document.Accounts = document.Accounts.Where(IsValid).ToList();
            if (document.Accounts.Count == 0)
            {
                var replacement = Guid.NewGuid();
                var settingsFile = Path.Combine("accounts", replacement.ToString("N") + ".ini");
                CopyLegacySettings(settingsFile);
                document.Accounts.Add(new AccountRegistryRecord
                {
                    AccountId = replacement,
                    DisplayName = "Default",
                    SettingsFileName = settingsFile,
                    BrowserProfileName = LegacyProfileName
                });
            }
            if (document.Accounts.All(item => item.AccountId != document.SelectedAccountId))
                document.SelectedAccountId = document.Accounts[0].AccountId;
        }

        private static bool IsValid(AccountRegistryRecord record)
        {
            return record != null && record.AccountId != Guid.Empty &&
                   !string.IsNullOrWhiteSpace(record.DisplayName) &&
                   !string.IsNullOrWhiteSpace(record.SettingsFileName) &&
                   !string.IsNullOrWhiteSpace(record.BrowserProfileName);
        }

        private AccountRegistrySnapshot CreateSnapshot()
        {
            return new AccountRegistrySnapshot(
                _document.Accounts.Select(ToDefinition).ToArray(),
                _document.SelectedAccountId);
        }

        private void SaveDocument()
        {
            Directory.CreateDirectory(SettingsRoot);
            var json = JsonSerializer.Serialize(_document, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_registryPath, json);
        }

        private static AccountDefinition ToDefinition(AccountRegistryRecord record)
        {
            return new AccountDefinition(
                record.AccountId,
                record.DisplayName,
                record.SettingsFileName,
                record.BrowserProfileName);
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
