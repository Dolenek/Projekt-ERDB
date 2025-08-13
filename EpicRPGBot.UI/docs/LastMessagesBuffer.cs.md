# LastMessagesBuffer.cs

Purpose
- Maintains a rolling buffer of the most recent N chat messages for display in the left “Last messages” panel.
- Exposes an ObservableCollection<MessageItem> for direct WPF binding.

Files
- [EpicRPGBot.UI/Services/LastMessagesBuffer.cs](EpicRPGBot.UI/Services/LastMessagesBuffer.cs:1)
- [EpicRPGBot.UI/Models/MessageItem.cs](EpicRPGBot.UI/Models/MessageItem.cs:1)
- [EpicRPGBot.UI/Services/UiDispatcher.cs](EpicRPGBot.UI/Services/UiDispatcher.cs:1)
- UI usage in [EpicRPGBot.UI/MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1) and [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1)

Public API
- LastMessagesBuffer(int capacity)
  - Capacity defines how many recent messages are kept (UI uses 5 by default).
- ObservableCollection<MessageItem> Items
  - Bind directly to a ListBox/ItemsControl (StatsList in the “Last messages” panel).
- void Add(string text)
  - Inserts the newest message at index 0 (newest-first, top of the list).
  - Automatically trims the oldest entries when over Capacity.
- void Clear()
  - Empties the collection.

Behavior
- Newest-first ordering: Add() inserts at the beginning so the most recent messages appear at the top.
- Capacity-bounded: when the collection exceeds Capacity, the oldest items (at the end) are removed.

Threading notes
- Items is an ObservableCollection bound to WPF UI and must be updated on the UI thread.
- From timers/async/event callbacks, marshal to UI with UiDispatcher.OnUI(() => buffer.Add(...)).

Typical usage
- In [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1):
  - private readonly LastMessagesBuffer _last = new LastMessagesBuffer(5);
  - StatsList.ItemsSource = _last.Items; // displayed in the “Last messages” tab/panel
  - When new text is detected (via DOM polling or engine events), call:
    - UiDispatcher.OnUI(_last.Add, messageText);

Related
- Message model: [EpicRPGBot.UI/Models/MessageItem.cs](EpicRPGBot.UI/Models/MessageItem.cs:1) — holds Timestamp + Text and formats as “[HH:mm:ss] Text”.
- UI panel:
  - “Last messages” tab/panel defined in [EpicRPGBot.UI/MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1) with ListBox name StatsList.