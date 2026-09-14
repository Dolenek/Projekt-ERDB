#nullable disable

using System;

namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountDefinition
    {
        public AccountDefinition(Guid accountId, string displayName, string settingsFileName, string browserProfileName)
        {
            if (accountId == Guid.Empty) throw new ArgumentException("Account id is required.", nameof(accountId));
            AccountId = accountId;
            DisplayName = NormalizeDisplayName(displayName);
            SettingsFileName = settingsFileName ?? throw new ArgumentNullException(nameof(settingsFileName));
            BrowserProfileName = browserProfileName ?? throw new ArgumentNullException(nameof(browserProfileName));
        }

        public Guid AccountId { get; }
        public string DisplayName { get; private set; }
        public string SettingsFileName { get; }
        public string BrowserProfileName { get; }

        public void Rename(string displayName)
        {
            DisplayName = NormalizeDisplayName(displayName);
        }

        private static string NormalizeDisplayName(string displayName)
        {
            var normalized = (displayName ?? string.Empty).Trim();
            if (normalized.Length == 0) throw new ArgumentException("Account name is required.", nameof(displayName));
            return normalized.Length <= 40 ? normalized : normalized.Substring(0, 40);
        }
    }
}
