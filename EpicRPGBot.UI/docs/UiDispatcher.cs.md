# UiDispatcher.cs

Small helper to marshal actions onto the WPF UI thread. Prevents InvalidOperationException when updating bound collections from background code.

Summary
- Static class with a single method OnUI(Action action).
- If Application.Current?.Dispatcher is available:
  - If already on UI thread: invoke synchronously.
  - Otherwise: BeginInvoke at DispatcherPriority.Background.
- If no dispatcher (during app shutdown/edge cases), runs the action directly as a best-effort fallback.

Key API
- public static void OnUI(Action action)

Typical Usage
- Updating ObservableCollection bound to UI from timers, WebView2 callbacks, or background tasks:
  UiDispatcher.OnUI(() => MyCollection.Add(item));

Notes
- Keeps UI responsive by using BeginInvoke.
- Safe to call frequently; avoid long-running work inside the action (do that off-UI, then marshal only the minimal UI update).