# Testing Automation

`EpicRPGBot.Mcp` is a Windows-only MCP sidecar for local end-to-end testing of `EpicRPGBot.UI`.

Runtime model:
- The MCP server is a separate `net8.0-windows` process.
- It builds and launches `EpicRPGBot.UI` itself instead of attaching to an arbitrary running app.
- The UI is started with `--automation`, `--automation-debug-port`, and `--automation-session`.
- Automation mode changes the window title to an automation-specific title and enables WebView2 remote debugging.

Native app tools:
- `launch_app` builds and starts the WPF app in automation mode.
- `close_app` closes the app started by the MCP server.
- `bring_to_front` restores and foregrounds the main window.
- `capture_window` saves a screenshot of the full app window and returns the image path.
- `list_controls` returns the discoverable WPF controls with stable automation IDs.
- `click_control`, `set_text`, and `get_text` operate on those WPF controls.
- `read_console` reads the Console tab list.
- `read_last_messages` reads the Last messages tab list.

WebView tools:
- `webview_eval` evaluates JavaScript in the Discord WebView through the WebView2 DevTools endpoint.
- `webview_capture` saves a DevTools screenshot of the Discord page and returns the image path.

Stable control IDs exposed by the UI:
- `StartButton`, `StopButton`, `InitializeButton`
- `ChannelUrlInput`, `UseAtMeFallback`, `AreaInput`
- `HuntCooldownInput`, `AdventureCooldownInput`, `WorkCooldownInput`, `FarmCooldownInput`
- `ReloadButton`, `GoChannelButton`, `DiscordWebView`
- `ConsoleList`, `LastMessagesList`, `CooldownsPanel`

Current assumptions:
- The first version is for an interactive Windows desktop session only.
- Discord authentication is still manual; the MCP server automates the already-logged-in embedded session.
- Live Discord actions are allowed; there is no dedicated safe-mode channel restriction in the current implementation.
