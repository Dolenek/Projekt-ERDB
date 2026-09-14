namespace EpicRPGBot.UI.Accounts
{
    public interface IAccountRuntimeFactory
    {
        AccountRuntime Create(AccountDefinition definition, string settingsPath, AccountBrowserHosts hosts);
    }
}
