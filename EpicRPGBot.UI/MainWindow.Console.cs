using System.Linq;
using System.Windows;
using System.Windows.Input;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI
{
    public partial class MainWindow
    {
        private void ConsoleList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.C || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return;
            }

            var entries = ConsoleList.SelectedItems
                .OfType<LogEntry>()
                .Select(entry => entry.ToString())
                .ToList();
            if (entries.Count == 0 && ConsoleList.SelectedItem is LogEntry singleEntry)
            {
                entries.Add(singleEntry.ToString());
            }

            if (entries.Count == 0)
            {
                return;
            }

            Clipboard.SetText(string.Join(System.Environment.NewLine, entries));
            _log.Info($"Copied {entries.Count} console line(s) to clipboard.");
            e.Handled = true;
        }
    }
}
