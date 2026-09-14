#nullable disable

using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.Accounts
{
    internal sealed class AccountRegistryDocument
    {
        public AccountRegistryDocument() { }
        public int Version { get; set; }
        public Guid SelectedAccountId { get; set; }
        public List<AccountRegistryRecord> Accounts { get; set; }
    }

    internal sealed class AccountRegistryRecord
    {
        public AccountRegistryRecord() { }
        public Guid AccountId { get; set; }
        public string DisplayName { get; set; }
        public string SettingsFileName { get; set; }
        public string BrowserProfileName { get; set; }
    }
}
