# Testing Automation

`EpicRPGBot.Mcp` is a Windows-only MCP sidecar for local end-to-end testing of `EpicRPGBot.UI`.

Codex connection from Windows PowerShell:
- Build the Windows server from the repository root:
  `dotnet build EpicRPGBot.Mcp/EpicRPGBot.Mcp.csproj -c Release`.
- Register the stdio server with:
  `codex mcp add epicrpg -- 'C:\Program Files\dotnet\dotnet.exe' 'C:\ERPGBOT\EpicRPGBotCSHARP\EpicRPGBot.Mcp\bin\Release\net8.0-windows\EpicRPGBot.Mcp.dll'`.
- This registration is stored in the user's `~/.codex/config.toml`; adjust the repository path for other checkouts.
- Codex desktop and CLI share this configuration. Restart Codex after registration to load the server tools.
- Verify registration with `codex mcp get epicrpg`. Start a new Codex session if the current session does not expose the tools.
- The server uses Windows .NET 8 and the interactive Windows desktop. Starting the server alone does not launch the bot; `launch_app` starts its managed instance.
- Rebuild the server after changing its source. Keep its build output inside this checkout so repository discovery can locate the solution.
- Connection reference: [Codex MCP configuration](https://developers.openai.com/codex/mcp).

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

Memory verification:

- Start the intended 1, 3, or 5 accounts and leave them running until browser activity settles.
- Run `tools/Measure-MultiAccountMemory.ps1 -RootProcessId <pid> -AccountCount <1|3|5>`.
- The result reports average and peak working set for the UI process tree and its WebView2 subset. Use `-OutputPath` to retain all samples as CSV.
- Repeat the same sample after rapid account switching to detect retained renderer processes or a rising steady-state working set.

WebView tools require the stable account id shown in the title-bar account
tooltip. This keeps Bot targets unambiguous when several accounts are active.
Use `list_accounts` to retrieve the same ids without inspecting the window.

WebView tools:
- `webview_eval` evaluates JavaScript in the bot Discord WebView through the WebView2 DevTools endpoint.
- `webview_capture` saves a DevTools screenshot of the bot Discord page and returns the image path.
- `read_webview_debug_state` returns URL, title, ready state, tab role, and a short body-text preview.
- `read_recent_webview_messages` returns parsed recent Discord message snapshots with `id`, `author`, `text`, rendered body text, and visible button metadata.
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
- `RpgCdButton`, `TradeAreaButton`, `WishingTokenButton`
- `DismantleButton`, `CraftingButton`, `SettingsButton`, `ReloadButton`, `GoChannelButton`
- `DiscordStatusText`, `EngineStatusText`
- `ActivitySearchInput`, `ActivityKindFilter`, `ActivityCollapseButton`, `ActivityExpandButton`
- `ControlCenterCollapseButton`, `ControlCenterExpandButton`
- `MinimizeButton`, `MaximizeRestoreButton`, `CloseWindowButton`
- `TimeCookieDungeonButton`, `TimeCookieDuelButton`, `TimeCookieCardHandButton`, `SleepyPotionButton`
- `HuntCountStat`, `AdventureCountStat`, `TrainingCountStat`, `WorkCountStat`, `FarmCountStat`, `LootboxCountStat`
- `RunningCooldownsStat`, `RunningRewardsStat`, `RunningExperienceStat`, `RunningProgressStat`
- `BrowserTabs`, `BotBrowserTab`, `PlayerBrowserTab`, `GuildBrowserTab`, `DungeonBrowserTab`, `DuelBrowserTab`
- `DiscordWebView`, `PlayerDiscordWebView`, `GuildDiscordWebView`, `DungeonDiscordWebView`, `DuelDiscordWebView`
- `ConsoleList`, `LastMessagesList`, `CooldownsPanel`
- `SettingsWindow`, `SettingsCloseButton`, `SettingsCardHandButton`
- `SettingsMinimizeButton`, `SettingsMaximizeRestoreButton`, `SettingsWindowCloseButton`
- `SettingsChannelUrlInput`, `SettingsDungeonListingChannelUrlInput`, `SettingsUseAtMeFallback`, `SettingsAreaInput`
- `SettingsHuntCooldownInput`, `SettingsAdventureCooldownInput`, `SettingsWorkCooldownInput`
- `SettingsTrainingCooldownInput`
- `SettingsFarmCooldownInput`, `SettingsLootboxCooldownInput`
- `SettingsGuildRaidButton`
- `CardHandSettingsWindow`, `CardHandAutoPlayInput`, `CardHandLoadDeckButton`, `CardHandDeckStatus`, `CardHandSettingsCloseButton`
- `CardHandSettingsMinimizeButton`, `CardHandSettingsMaximizeRestoreButton`, `CardHandSettingsWindowCloseButton`
- `CardHandWeight{Reward}` for each of the nine reward types
- `GuildRaidSettingsWindow`, `GuildRaidSettingsActiveInput`, `GuildRaidSettingsChannelUrlInput`, `GuildRaidSettingsTriggerInput`, `GuildRaidSettingsMatchModeInput`, `GuildRaidSettingsAuthorFilterInput`, `GuildRaidSettingsCloseButton`
- `GuildRaidSettingsMinimizeButton`, `GuildRaidSettingsMaximizeRestoreButton`, `GuildRaidSettingsWindowCloseButton`
- `WorkCommandsWindow`, `WorkCommandsCloseButton`, `WorkCommandsMinimizeButton`, `WorkCommandsMaximizeRestoreButton`, `WorkCommandsWindowCloseButton`
- `CraftingWindow`, `CraftEpicAmountInput`, `CraftSuperAmountInput`, `CraftMegaAmountInput`, `CraftHyperAmountInput`, `CraftUltraAmountInput`
- `CraftEpicFishAmountInput`, `CraftGoldenFishAmountInput`, `CraftBananaAmountInput`
- `CraftStatusText`, `CraftStartButton`, `CraftCancelButton`
- `DismantleWindow`, `DismantleUltraAmountInput`, `DismantleHyperAmountInput`, `DismantleMegaAmountInput`, `DismantleSuperAmountInput`, `DismantleEpicAmountInput`
- `DismantleEpicFishAmountInput`, `DismantleGoldenFishAmountInput`, `DismantleBananaAmountInput`
- `DismantleStatusText`, `DismantleStartButton`, `DismantleCancelButton`

Current assumptions:
- The first version is for an interactive Windows desktop session only.
- Discord authentication is still manual; the MCP server automates the already-logged-in embedded session.
- Live Discord actions are allowed; there is no dedicated safe-mode channel restriction in the current implementation.
- Bot and Player remain available as DevTools targets. Guild, Dungeon, and Duel targets exist only while selected or held by a workflow/watcher; target selection resolves each page via its injected tab-role marker.

Shared-profile constraint:
- Close the regular UI instance before launching the MCP-managed UI. Both currently use the same WebView2 user-data folder. Different debugging options on simultaneous instances can cause initialization failure `0x8007139F`. Restart the MCP-managed instance after releasing the profile.

Common recovery steps:
- If the WebView looks blank, call `read_webview_debug_state` first; if the URL/title are valid, prefer WebView message tools over relying on the window screenshot alone.
- If a sidebar list read fails, use `get_app_status` and then retry the list tool; it now selects the expected tab before reading.
- If a WebView tool reports target-resolution failure, relaunch the automation app instance so the DevTools port and tab-role markers are recreated.
