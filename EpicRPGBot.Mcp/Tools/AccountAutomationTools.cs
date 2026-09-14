using System.ComponentModel;
using System.IO;
using System.Text.Json;
using EpicRPGBot.Mcp.Models;
using ModelContextProtocol.Server;

namespace EpicRPGBot.Mcp.Tools;

[McpServerToolType]
public sealed class AccountAutomationTools
{
    [McpServerTool, Description("List configured EpicRPGBot accounts and their stable ids for account-scoped WebView tools.")]
    public AccountListResult list_accounts()
    {
        try
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EpicRPGBot.UI", "settings", "accounts.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            var selectedId = ReadString(root, "SelectedAccountId");
            var accounts = root.GetProperty("Accounts").EnumerateArray()
                .Select(item => CreateSummary(item, selectedId))
                .ToArray();
            return new AccountListResult(accounts);
        }
        catch (Exception ex)
        {
            return new AccountListResult(Array.Empty<AccountSummaryResult>(), false, ex.Message);
        }
    }

    private static AccountSummaryResult CreateSummary(JsonElement account, string selectedId)
    {
        var accountId = ReadString(account, "AccountId");
        var displayName = ReadString(account, "DisplayName");
        return new AccountSummaryResult(accountId, displayName,
            string.Equals(accountId, selectedId, StringComparison.OrdinalIgnoreCase));
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property)
            ? property.ToString()
            : string.Empty;
    }
}
