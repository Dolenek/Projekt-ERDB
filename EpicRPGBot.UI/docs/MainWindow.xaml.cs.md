# MainWindow.xaml.cs

Purpose
- WPF code-behind for the main window.
- Owns UI state (left Last messages/Stats/Console panels, settings text boxes) and interacts with the engine.
- Hosts and initializes WebView2, navigates to the Discord channel, handles interstitials.
- Polls the page for the last chat message to populate the “Last messages” pane.
- Subscribes to bot send events to:
  - Log “Message (message) sent” in Console.
  - Maintain a Stats counter for “rpg hunt” sends.

Files
- [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1)
- [EpicRPGBot.UI/MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1)
- [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1)

Key UI elements (by name)
- Left tab buttons:
  - LastMessagesTabBtn, StatsTabBtn, ConsoleTabBtn: toggle which left panel is visible.
- Left panels:
  - LastMessagesPanel: shows recent chat messages (StatsList) — rolling buffer of 5.
  - StatsPanel: shows counters like HuntCountText (integer counter for “rpg hunt” sends).
  - ConsolePanel: shows log lines (ConsoleList).
- Center:
  - Web (WebView2): central browser surface for Discord.
  - InitHint: simple overlay text during WebView2 initialization.
- Right settings:
  - ChannelUrlBox, UseAtMeFallback: navigation target controls.
  - AreaBox, HuntCdBox, WorkCdBox, FarmCdBox: bot parameters.
  - StartBtn, StopBtn: engine controls.

Initialization flow
1) MainWindow_Loaded():
   - Loads .env and seeds UI values (DISCORD_CHANNEL_URL, AREA, HUNT/WORK/FARM_COOLDOWN).
   - Binds Last messages list: StatsList.ItemsSource = LastMessagesBuffer.Items.
   - Binds Console list: ConsoleList.ItemsSource = InMemoryLog.Items.
   - Initializes WebView2 with a persistent user data folder under LocalAppData/EpicRPGBot.UI/WebView2.
   - Navigates to channel URL (from textbox or fallback to @me).
   - Starts polling the last message every 2 seconds (deduped on UI side).
   - Caches named elements via FindName:
     - LastMessagesPanel, HuntCountText (for tab toggling and Stats updates).

WebView2 setup
- CoreWebView2 settings enable context menus, dev tools, and zoom control.
- NavigationCompleted triggers an interstitial click helper that presses “Open/Continue in browser” prompts when present.

Discord interstitial helper
- ClickInterstitialsAsync scans visible text for:
  - “Open Discord in your browser”, “Continue to Discord”, “Continue in browser”
  - Clicks them if present.

Polling last messages
- StartPollingLastMessage uses a DispatcherTimer to:
  - Evaluate DOM (li[id^='chat-messages-']) for the last item’s innerText.
  - Deduplicate on the UI side to avoid re-adding the same last line.
  - Append to the rolling LastMessagesBuffer (size 5) via UiDispatcher.

Start/Stop behavior
- StartBtn_Click (async):
  - If an engine instance exists and IsRunning == true, ignore the click.
  - Reads area/cooldowns from text boxes.
  - Creates a new BotEngine with those parameters.
  - Subscribes to the engine’s send event BEFORE any sends:
    - OnCommandSent: logs via InMemoryLog.Command => “Message (cmd) sent”
    - If cmd starts with “rpg hunt” (case-insensitive), increments HuntCountText value on UI.
  - Immediately sends “rpg cd” via engine.SendImmediateAsync("rpg cd") and logs success/failure to Console.
  - Calls engine.Start() to begin timers and opening sequence (hunt/work[/farm]).
- StopBtn_Click:
  - Calls engine.Stop() and logs the stop event.

Left sidebar toggle
- LastMessagesTabBtn_Click(), StatsTabBtn_Click(), ConsoleTabBtn_Click():
  - Toggle Visibility among LastMessagesPanel, StatsPanel, ConsolePanel.

Data sources and helpers
- InMemoryLog: appends Engine/Info/Command log lines with timestamps; bound to ConsoleList.
- LastMessagesBuffer: rolling last 5 chat messages with timestamps; bound to StatsList.
- UiDispatcher.OnUI is used whenever UI-bound collections or TextBlocks are updated from async/timer/event callbacks.

Related services and models
- [EpicRPGBot.UI/Services/InMemoryLog.cs](EpicRPGBot.UI/Services/InMemoryLog.cs:1) — in-memory console log (ConsoleList binding).
- [EpicRPGBot.UI/Services/LastMessagesBuffer.cs](EpicRPGBot.UI/Services/LastMessagesBuffer.cs:1) — rolling buffer for “Last messages” pane.
- [EpicRPGBot.UI/Services/UiDispatcher.cs](EpicRPGBot.UI/Services/UiDispatcher.cs:1) — marshals updates to the WPF UI thread.

Notes
- The engine’s OnCommandSent is subscribed before the immediate “rpg cd” send to ensure every bot send is logged and counted.
- The Stats counter matches any command that starts with “rpg hunt” to include variants like “rpg hunt h”.