using System.Windows;
using System.Windows.Controls;

namespace EpicRPGBot.UI.Accounts
{
    public sealed class AccountNameDialog : Window
    {
        private readonly TextBox _nameInput;

        public AccountNameDialog(string title, string initialName = "")
        {
            Title = title;
            Width = 360;
            Height = 165;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            _nameInput = new TextBox { Text = initialName ?? string.Empty, Margin = new Thickness(0, 8, 0, 16) };
            Content = BuildContent();
            Loaded += (sender, args) =>
            {
                _nameInput.Focus();
                _nameInput.SelectAll();
            };
        }

        public string AccountName => _nameInput.Text?.Trim() ?? string.Empty;

        private UIElement BuildContent()
        {
            var panel = new StackPanel { Margin = new Thickness(18) };
            panel.Children.Add(new TextBlock { Text = "Account name" });
            panel.Children.Add(_nameInput);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var cancel = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true };
            var confirm = new Button { Content = "Save", MinWidth = 80, Margin = new Thickness(8, 0, 0, 0), IsDefault = true };
            confirm.Click += Confirm_Click;
            buttons.Children.Add(cancel);
            buttons.Children.Add(confirm);
            panel.Children.Add(buttons);
            return panel;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AccountName))
            {
                MessageBox.Show(this, "Enter an account name.", "Account name",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
        }
    }
}
