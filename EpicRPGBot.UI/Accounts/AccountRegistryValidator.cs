#nullable disable

using System;
using System.Collections.Generic;
using System.IO;

namespace EpicRPGBot.UI.Accounts
{
    internal sealed class AccountRegistryValidator
    {
        private readonly string _settingsRoot;

        public AccountRegistryValidator(string settingsRoot)
        {
            _settingsRoot = Path.GetFullPath(settingsRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        public void Validate(AccountRegistryDocument document)
        {
            if (document.Version != 1) throw InvalidRegistry("Unsupported registry version.");
            if (document.Accounts == null || document.Accounts.Count == 0)
                throw InvalidRegistry("At least one account is required.");
            var accountIds = new HashSet<Guid>();
            var settingsPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var profileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var record in document.Accounts)
            {
                ValidateRecord(record);
                if (!accountIds.Add(record.AccountId) ||
                    !settingsPaths.Add(ResolveSettingsPath(record.SettingsFileName)) ||
                    !profileNames.Add(record.BrowserProfileName))
                    throw InvalidRegistry("Account ids, settings files and browser profiles must be unique.");
            }
        }

        public string ResolveSettingsPath(string settingsFileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settingsFileName) || Path.IsPathRooted(settingsFileName))
                    throw InvalidRegistry("Account settings paths must be relative.");
                var candidate = Path.GetFullPath(Path.Combine(_settingsRoot, settingsFileName));
                if (!candidate.StartsWith(_settingsRoot, StringComparison.OrdinalIgnoreCase))
                    throw InvalidRegistry("Account settings path leaves the settings directory.");
                return candidate;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                throw InvalidRegistry("Account settings path is invalid.", exception);
            }
        }

        private void ValidateRecord(AccountRegistryRecord record)
        {
            if (record == null || record.AccountId == Guid.Empty || string.IsNullOrWhiteSpace(record.DisplayName) ||
                string.IsNullOrWhiteSpace(record.SettingsFileName) || string.IsNullOrWhiteSpace(record.BrowserProfileName))
                throw InvalidRegistry("An account is missing its id, name, settings file or browser profile.");
        }

        private InvalidDataException InvalidRegistry(string reason, Exception exception = null)
        {
            return new InvalidDataException($"Account registry '{Path.Combine(_settingsRoot, "accounts.json")}': {reason}", exception);
        }
    }
}
