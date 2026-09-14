using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using EpicRPGBot.UI.Accounts;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void RefreshAccountStrip()
        {
            if (AccountStrip == null) return;
            AccountStrip.Children.Clear();
            foreach (var runtime in _accountRuntimes)
            {
                AccountStrip.Children.Add(CreateAccountButton(runtime));
            }
        }

        private Button CreateAccountButton(AccountRuntime runtime)
        {
            var button = new Button
            {
                Content = BuildAccountLabel(runtime),
                Tag = runtime,
                ToolTip = $"{runtime.Definition.DisplayName}\nAccountId: {runtime.Definition.AccountId:D}",
                Margin = new Thickness(0, 0, 5, 0),
                Padding = new Thickness(10, 5, 10, 5),
                MinWidth = 105,
                Style = TryFindResource("TitleBarButtonStyle") as Style
            };
            if (ReferenceEquals(runtime, _activeAccountRuntime))
                button.Background = FindStatusBrush("AccentBrush");
            AutomationProperties.SetAutomationId(button,
                "Account-" + runtime.Definition.AccountId.ToString("D"));
            AutomationProperties.SetHelpText(button, runtime.Definition.AccountId.ToString("D"));
            button.Click += AccountButton_Click;
            var rename = new MenuItem { Header = "Rename", Tag = runtime };
            rename.Click += RenameAccount_Click;
            button.ContextMenu = new ContextMenu();
            button.ContextMenu.Items.Add(rename);
            return button;
        }

        private static string BuildAccountLabel(AccountRuntime runtime)
        {
            var isRunning = runtime.Engine?.IsRunning == true;
            var status = isRunning ? "Running" : "Stopped";
            var operation = string.IsNullOrWhiteSpace(runtime.ActiveExclusiveBotOperation)
                ? string.Empty
                : " — " + runtime.ActiveExclusiveBotOperation;
            return $"{status} • {runtime.Definition.DisplayName}{operation}";
        }
    }
}
