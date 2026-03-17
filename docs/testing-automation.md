# Testing Automation

`EpicRPGBot.Mcp` is a Windows-only MCP sidecar for local end-to-end testing of `EpicRPGBot.UI`.

Runtime model:
- The MCP server is a separate `net8.0-windows` process.
- It builds and launches `EpicRPGBot.UI` itself instead of attaching to an arbitrary running app.
- The UI is started with `--automation`, `--automation-debug-port`, and `--automation-session`.
- Automation mode changes the window title to an automation-specific title and enables WebView2 remote debugging.
- The server is designed to control the app instance it launched itself; it does not attach to an arbitrary already-running window.

Native app tools:
- `launch_app` builds and starts the WPF app in automation mode.
- `get_app_status` returns the current MCP-managed app status without launching or focusing it.
- `close_app` closes the app started by the MCP server.
- `bring_to_front` restores and foregrounds the main window.
- `capture_window` saves a screenshot of the full app window and returns the image path.
- `list_controls` returns the discoverable WPF controls with stable automation IDs.
- `click_control`, `set_text`, and `get_text` operate on those WPF controls.
- `read_console` selects the Console tab, then reads the log list.
- `read_last_messages` selects the Last messages tab, then reads the list.
- `wait_for_control_text` waits until a WPF control text contains a target substring.

WebView tools:
- `webview_eval` evaluates JavaScript in the bot Discord WebView through the WebView2 DevTools endpoint.
- `webview_capture` saves a DevTools screenshot of the bot Discord page and returns the image path.
- `read_webview_debug_state` returns URL, title, ready state, tab role, and a short body-text preview.
- `read_recent_webview_messages` returns parsed recent Discord message snapshots with `id`, `author`, and `text`.
- `wait_for_webview_message` waits for a Discord message matching author/text filters and optional `afterId`.

Result behavior:
- Read and wait tools return structured payloads with `success`, `error`, and current app `status` for common session/control issues.
- Empty lists are returned as successful reads when the list exists but has no items.
- Tool failures should distinguish app-not-running, control-not-found, and WebView-target-resolution problems.

Recommended debugging flow:
1. `launch_app` or `get_app_status`
2. `list_controls` when you need stable automation ids
3. `read_webview_debug_state` to confirm the bot WebView is loaded and targeting the expected page
4. `click_control` / `set_text`
5. `wait_for_control_text` or `wait_for_webview_message` instead of fixed sleeps
6. `read_recent_webview_messages` or `webview_eval` when deeper DOM inspection is needed

Stable control IDs exposed by the UI:
- `StartButton`, `StopButton`, `InitializeButton`
- `SettingsButton`, `ReloadButton`, `GoChannelButton`
- `HuntCountStat`, `AdventureCountStat`, `WorkCountStat`, `FarmCountStat`, `LootboxCountStat`
- `RunningCooldownsStat`, `RunningRewardsStat`, `RunningExperienceStat`, `RunningProgressStat`
- `BrowserTabs`, `BotBrowserTab`, `PlayerBrowserTab`
- `DiscordWebView`, `PlayerDiscordWebView`
- `ConsoleList`, `LastMessagesList`, `CooldownsPanel`
- `SettingsWindow`, `SettingsCloseButton`
- `SettingsChannelUrlInput`, `SettingsUseAtMeFallback`, `SettingsAreaInput`
- `SettingsHuntCooldownInput`, `SettingsAdventureCooldownInput`, `SettingsWorkCooldownInput`
- `SettingsFarmCooldownInput`, `SettingsLootboxCooldownInput`

Current assumptions:
- The first version is for an interactive Windows desktop session only.
- Discord authentication is still manual; the MCP server automates the already-logged-in embedded session.
- Live Discord actions are allowed; there is no dedicated safe-mode channel restriction in the current implementation.
- When two Discord tabs are open, MCP DevTools selection resolves the bot page via an injected tab-role marker instead of assuming the first Discord target is correct.

Common recovery steps:
- If the WebView looks blank, call `read_webview_debug_state` first; if the URL/title are valid, prefer WebView message tools over relying on the window screenshot alone.
- If a sidebar list read fails, use `get_app_status` and then retry the list tool; it now selects the expected tab before reading.
- If a WebView tool reports target-resolution failure, relaunch the automation app instance so the DevTools port and tab-role markers are recreated.
