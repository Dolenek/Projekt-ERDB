# InMemoryLog.cs

Purpose
- Lightweight, in-memory console log for the UI.
- Exposes an ObservableCollection<LogEntry> for direct data binding in WPF (ConsoleList.ItemsSource = _log.Items).

Key Types
- LogEntry (Models/LogEntry.cs)
  - Timestamp (DateTime)
  - Kind (string) — e.g., "info", "warn", "error", "engine"
  - Message (string)

Public API
- ObservableCollection<LogEntry> Items
  - Bind this to a ListBox/ItemsControl in XAML to render the console-like view.
- int MaxItems (default: 500)
  - Rolling cap to prevent unbounded memory growth.
- void Append(LogEntry entry)
  - Appends a new entry and prunes if over MaxItems.
- void Info(string message)
- void Warn(string message)
- void Error(string message)
- void Engine(string message)
  - Convenience methods that create a LogEntry with the given kind.

Threading Notes
- Items is a WPF-bound ObservableCollection and must be updated on the UI thread.
- Use UiDispatcher.OnUI(() => _log.Info("...")) if you are appending from a non-UI context.

Typical Usage
- In MainWindow.xaml.cs:
  - ConsoleList.ItemsSource = _log.Items
  - _log.Info("Start button clicked");
  - _log.Engine("Engine started (timers running; hunt/work[/farm] scheduled)");

Behavior
- Each append creates a LogEntry with a UTC timestamp.
- When the collection size exceeds MaxItems, the oldest items are removed.

Extending
- Add more convenience methods if you need additional categories (e.g., Debug).
- Consider persisting to disk if you need historical logs (this class intentionally keeps only an in-memory rolling buffer).

Related Files
- Models/LogEntry.cs — log record model.
- Services/UiDispatcher.cs — helper to marshal actions onto the WPF UI thread.
- MainWindow.xaml(.cs) — binds ConsoleList.ItemsSource to InMemoryLog.Items and writes log lines on user actions/engine events.