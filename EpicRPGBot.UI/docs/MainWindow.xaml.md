# MainWindow.xaml

Defines the 3-column layout of the EpicRPGBot.UI application.

Layout
- Columns
  - Left: 260px sidebar with three tabs and two content panels:
    - Tabs: “Last messages”, “Stats”, “Console”.
    - Panels:
      - LastMessagesPanel: ListBox (StatsList) bound to the rolling last 5 messages.
      - StatsPanel: simple counters (e.g., “Hunt sent: N”).
      - ConsolePanel: ListBox (ConsoleList) bound to log entries.
  - Center: WebView2 host (Discord) with a small header row containing navigation buttons.
  - Right: 300px settings panel including channel URL, bot parameters and Start/Stop buttons.

Key named elements
- LastMessagesTabBtn / StatsTabBtn / ConsoleTabBtn: toggle which panel is visible in the left column.
- LastMessagesPanel: shows recent chat messages (StatsList).
- StatsPanel: shows counters like HuntCountText.
- ConsolePanel: shows log lines (ConsoleList).
- Web: WebView2 control for Discord.
- InitHint: simple overlay text shown during WebView2 initialization.
- ChannelUrlBox, UseAtMeFallback: channel navigation controls.
- AreaBox, HuntCdBox, WorkCdBox, FarmCdBox: bot parameter inputs.
- StartBtn, StopBtn: engine controls.

Notes
- Uses a dark theme palette aligned loosely with Discord.
- Top row in the center provides Reload and “Go To Channel” actions.
- XAML names are referenced in code-behind for logic and bindings.