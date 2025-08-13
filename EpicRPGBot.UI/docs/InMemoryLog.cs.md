# InMemoryLog.cs

Purpose
- Lightweight, in-memory console log for the UI.
- Exposes an ObservableCollection<LogEntry> for direct data binding in WPF (ConsoleList.ItemsSource = _log.Items).

Files
- [EpicRPGBot.UI/Services/InMemoryLog.cs](EpicRPGBot.UI/Services/InMemoryLog.cs:1)
- [EpicRPGBot.UI/Models/LogEntry.cs](EpicRPGBot.UI/Models/LogEntry.cs:1)
- [EpicRPGBot.UI/Services/UiDispatcher.cs](EpicRPGBot.UI/Services/UiDispatcher.cs:1)

Public API
- ObservableCollection<LogEntry> Items
  - Bind this to a ListBox/ItemsControl in XAML to render the console-like view.
- Methods (convenience)
  - Info(string message)
  - Command(string message)
  - Warning(string message)
  - Error(string message)
  - Engine(string message)
- Append(LogEntry entry)
  - Appends a new entry and prunes oldest items to keep at most 500.

Threading notes
- Items is a WPF-bound ObservableCollection and must be updated on the UI thread.
- Use UiDispatcher.OnUI(() => _log.Info("...")) if you are appending from a non-UI context.
- In the current UI, all engine event callbacks are marshaled to UI via UiDispatcher before logging.

Typical usage
- In [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1):
  - ConsoleList.ItemsSource = _log.Items
  - _log.Info("Start button clicked");
  - _log.Engine("Engine started (timers running; hunt/work[/farm] scheduled)");
  - On bot send event:
    - _log.Command($"Message ({cmd}) sent")

Behavior
- Each append creates a LogEntry with a timestamp and kind.
- When the collection size exceeds 500, the oldest items are removed (rolling buffer).

Extending
- Add more convenience methods if you need additional categories (e.g., Debug).
- Consider persisting to disk if you need historical logs (this class intentionally keeps only an in-memory rolling buffer).

Related
- The bot engine emits a send event; the UI subscribes and writes “Message (message) sent” via _log.Command. See [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs) and [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs).