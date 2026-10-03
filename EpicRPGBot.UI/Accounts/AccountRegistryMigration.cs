#nullable disable

using System;
using System.Collections.Generic;
using System.IO;

namespace EpicRPGBot.UI.Accounts
{
    internal static class AccountRegistryMigration
    {
        public static AccountRegistryDocument CreateDocument()
        {
            var accountId = Guid.NewGuid();
            return new AccountRegistryDocument
            {
                Version = 1, SelectedAccountId = accountId,
                Accounts = new List<AccountRegistryRecord>
                {
                    new AccountRegistryRecord
                    {
                        AccountId = accountId, DisplayName = "Default",
                        SettingsFileName = Path.Combine("accounts", accountId.ToString("N") + ".ini"),
                        BrowserProfileName = "Default"
                    }
                }
            };
        }

        public static void Save(string settingsRoot, AccountRegistryDocument document, IAccountRegistryStorage storage)
        {
            var copiedSettingsPath = CopyLegacySettings(settingsRoot, document.Accounts[0].SettingsFileName);
            try { storage.Write(document); }
            catch
            {
                if (copiedSettingsPath != null) File.Delete(copiedSettingsPath);
                throw;
            }
        }

        private static string CopyLegacySettings(string settingsRoot, string accountSettingsFile)
        {
            var sourcePath = Path.Combine(settingsRoot, "app-settings.ini");
            var destinationPath = Path.Combine(settingsRoot, accountSettingsFile);
            if (!File.Exists(sourcePath)) return null;
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
            File.Copy(sourcePath, destinationPath, false);
            return destinationPath;
        }
    }
}
