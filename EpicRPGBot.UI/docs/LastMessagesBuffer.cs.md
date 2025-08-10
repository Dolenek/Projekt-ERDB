# LastMessagesBuffer.cs

Purpose
- Maintains a rolling buffer of the most recent N chat messages for display in the left “Stats” panel.
- Exposes an ObservableCollection<MessageItem> for direct WPF binding.

Key Types
- MessageItem (Models/MessageItem.cs)
  - Timestamp (DateTime)
  - Text (string)

Public API
- LastMessagesBuffer(int capacity)
  - Capacity defines how many recent messages are kept (UI uses 5 by default).
- ObservableCollection<MessageItem> Items
  - Bind directly to a ListBox/ItemsControl (e.g., StatsList.ItemsSource).
- void Add(string text)
  - Pushes a new message into the buffer with current UTC timestamp.
  - Automatically trims the oldest entries when over capacity.
- void Clear()
  - Empties the collection.

Threading Notes
- Items is an ObservableCollection bound to the WPF UI. Append on the UI thread.
- If adding from timers or async background calls, marshal to UI with UiDispatcher.OnUI(() => buffer.Add(...)).

Typical Usage
- In MainWindow.xaml.cs:
  - private readonly LastMessagesBuffer _last = new LastMessagesBuffer(5);
  - StatsList.ItemsSource = _last.Items;
  - When new text is detected (via DOM polling or engine events), call _last.Add(text) on the UI thread.

Behavior
- Keeps at most Capacity messages.
- Adds newest message to the end of the observable collection; the ListBox shows them in order.

Extending
- Consider adding a filter or message source tag if you want to distinguish system vs. user vs. bot messages.
- Capacity is fixed per instance; add a SetCapacity method if you want runtime configuration.

Related Files
- Models/MessageItem.cs — data model with timestamp + text.
- Services/UiDispatcher.cs — ensures updates happen on the WPF UI thread.
- MainWindow.xaml(.cs) — binds the left Stats ListBox to LastMessagesBuffer.Items.