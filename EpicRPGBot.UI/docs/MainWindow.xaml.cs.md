# MainWindow.xaml.cs

Purpose
- WPF code-behind for the main window. 
- Owns UI state (left Stats/Console lists, settings text boxes) and interacts with BotEngine.
- Hosts and initializes WebView2, navigates to the Discord channel, handles interstitials.
- Polls the page for the last chat message to populate the Stats pane.

Key UI Elements (by name)
- Web (WebView2): Central browser surface for Discord.
- StatsList (ListBox/ItemsControl): Displays rolling last 5 messages.
- ConsoleList (ListBox/ItemsControl): Console-like logs of actions.
- ChannelUrlBox, UseAtMeFallback: Navigation target controls.
- AreaBox, HuntCdBox, WorkCdBox, FarmCdBox: Bot parameters.
- StartBtn, StopBtn: Bot control buttons.
- StatsTabBtn, ConsoleTabBtn: Toggle which left panel is visible.
- StatsPanel, ConsolePanel: Left-side containers, toggled visible/hidden.

Initialization flow
1) MainWindow_Loaded():
   - Env.Load() and seed UI values from .env (DISCORD_CHANNEL_URL, AREA, HUNT/WORK/FARM_COOLDOWN).
   - Bind StatsList to LastMessagesBuffer.Items and ConsoleList to InMemoryLog.Items.
   - Initialize WebView2 with a persistent user data folder under LocalAppData/EpicRPGBot.UI/WebView2.
   - Navigate to channel URL (from box or fallback to @me).
   - Start polling the last message every 2 seconds.

WebView2 setup
- CoreWebView2 settings enable context menus, dev tools, and zoom control.
- NavigationCompleted triggers an interstitial click helper that presses “Open/Continue in browser” prompts when present.

Discord interstitial helper
- ClickInterstitialsAsync() scans visible text for “Open Discord in your browser”, “Continue to Discord”, or “Continue in browser” and clicks them if found.

Polling last messages
- StartPollingLastMessage() uses a DispatcherTimer to:
  - Evaluate a small JS snippet to read the last li[id^='chat-messages-'] element’s innerText.
  - Deduplicate on the UI side to avoid re-adding the same last line.
  - Append to the rolling LastMessagesBuffer (size 5) via UiDispatcher.

Start/Stop behavior
- StartBtn_Click (async):
  - If a bot engine instance exists and IsRunning == true, ignore the click (no duplicate engines or “rpg cd” sends).
  - Reads area/cooldowns from text boxes.
  - Creates a new BotEngine with those parameters.
  - Immediately sends “rpg cd” via engine.SendImmediateAsync("rpg cd") and logs success/failure into the Console view.
  - Calls engine.Start() to begin timers and the opening hunt/work[/farm] sequence.
- StopBtn_Click:
  - Calls engine.Stop() and logs the stop event.

Left sidebar toggle
- StatsTabBtn_Click(), ConsoleTabBtn_Click():
  - Toggle visibility of StatsPanel and ConsolePanel.

Helpers
- GetChannelUrl(): resolves channel URL from textbox with optional @me fallback.
- UnquoteJson(): utility to unescape the string result from ExecuteScriptAsync.
- SafeInt(): simple parser with default.

Data sources (bound in code-behind)
- InMemoryLog: appends Engine/Info log lines with timestamps; bound to ConsoleList.
- LastMessagesBuffer: rolling last 5 chat messages with timestamps; bound to StatsList.

Notes
- The UI currently polls last messages on its own. The engine also emits OnMessageSeen, which can be wired for richer behavior in the future.
- If Discord markup changes, the page polling might require a small selector update (li[id^='chat-messages-']). The engine’s internal scanning uses a similar pattern.

Troubleshooting
- If Start does nothing:
  - Ensure WebView2 is initialized (InitHint is hidden).
  - Check that Discord is loaded and you are authenticated.
- If no messages appear in Stats:
  - Confirm the channel actually has messages; the polling appends only when the last item changes.