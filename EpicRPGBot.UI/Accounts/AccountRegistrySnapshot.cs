#nullable disable

using System;
using System.Collections.Generic;

namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountRegistrySnapshot
    {
        public AccountRegistrySnapshot(IReadOnlyList<AccountDefinition> accounts, Guid selectedAccountId)
        {
            Accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
            SelectedAccountId = selectedAccountId;
        }

        public IReadOnlyList<AccountDefinition> Accounts { get; }
        public Guid SelectedAccountId { get; }
    }
}
