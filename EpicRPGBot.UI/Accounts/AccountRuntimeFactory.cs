namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountRuntimeFactory : IAccountRuntimeFactory
    {
        public AccountRuntime Create(AccountDefinition definition, string settingsPath, AccountBrowserHosts hosts)
        {
            return new AccountRuntime(definition, settingsPath, hosts);
        }
    }
}
