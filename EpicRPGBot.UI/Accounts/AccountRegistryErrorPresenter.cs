using System;
using System.IO;
using System.Windows;

namespace EpicRPGBot.UI.Accounts
{
    internal static class AccountRegistryErrorPresenter
    {
        public static bool IsStorageError(Exception exception)
        {
            return exception is InvalidDataException || exception is IOException || exception is UnauthorizedAccessException;
        }

        public static void Show(Window owner, Exception exception)
        {
            var message = "The account registry could not be loaded or saved.\n\n" + exception.Message +
                "\n\nCheck accounts.json in the EpicRPGBot.UI settings directory and its file permissions. " +
                "An existing registry is never replaced with a new default account after a read error.";
            if (owner == null) MessageBox.Show(message, "Account registry", MessageBoxButton.OK, MessageBoxImage.Error);
            else MessageBox.Show(owner, message, "Account registry", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
