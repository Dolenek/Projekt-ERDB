using System.Collections.ObjectModel;
using EpicRPGBot.UI.Models;

namespace EpicRPGBot.UI.Services
{
    public sealed class InMemoryLog
    {
        public ObservableCollection<LogEntry> Items { get; } = new ObservableCollection<LogEntry>();

        public void Info(string message) => Append(new LogEntry(LogKind.Info, message));
        public void Command(string message) => Append(new LogEntry(LogKind.Command, message));
        public void Warning(string message) => Append(new LogEntry(LogKind.Warning, message));
        public void Error(string message) => Append(new LogEntry(LogKind.Error, message));
        public void Engine(string message) => Append(new LogEntry(LogKind.Engine, message));

        public void Append(LogEntry entry)
        {
            Items.Add(entry);
            // Optionally trim if needed
            if (Items.Count > 500)
            {
                // Keep last 500 lines
                while (Items.Count > 500)
                    Items.RemoveAt(0);
            }
        }
    }
}