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
  - Right: 300px settings panel including channel URL, bot parameters, Start/Stop buttons, and a “Cooldowns (visual)” list rendered under the Start/Stop buttons.

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
- Better Settings: The right panel shows a read-only “Cooldowns (visual)” section that lists common Epic RPG commands with placeholder timers:
  - Rewards: daily, weekly, lootbox, card hand, vote
  - Experience: hunt, adventure, training, duel, quest | epic quest
  - Progress: chop | fish | pickup | mine, farm, horse breeding | horse race, arena, dungeon | miniboss
## Inicialize button (concept for Hunt)

- Added an "Inicialize" button next to Start/Stop in the right panel in [MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1) with name InitBtn.
- Behavior implemented in [InitBtn_Click()](EpicRPGBot.UI/MainWindow.xaml.cs:1):
  - Sends "rpg hunt h".
  - Waits 2 seconds.
  - Sends "rpg cd".
  - Parses the resulting cooldowns message and reads the hunt remaining time.
  - Adds a 3-second safety margin to the parsed hunt time.
  - Persists this as milliseconds to a local settings file:
    - Path: %LocalAppData%/EpicRPGBot.UI/settings/cooldowns.ini
    - Format: hunt_ms=&lt;milliseconds&gt;
  - Updates the Hunt cooldown textbox (HuntCdBox) to this persisted value.

- On application load, if this file exists, the stored hunt_ms is used; otherwise a default of 61,000ms (1m 1s) is assumed and written into the UI textbox.

Notes
- This is currently implemented only for the "hunt" command as a concept for the full initialization flow to be extended to other cooldowns.
- Visual cooldown labels on the right are still driven by parsing of the "rpg cooldowns" message and a 1s ticking timer; initialization just establishes a persisted baseline for internal scheduling and UI.
## Inicialize button (multi-command setup)

- An "Inicialize" button sits next to Start/Stop in [MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1) (Name: InitBtn).
- Entry point: [InitBtn_Click()](EpicRPGBot.UI/MainWindow.xaml.cs:325)
- Helper used per command: InitializeOneAsync(...) in [MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:551)

Purpose
- Establish persisted baseline cooldowns by issuing commands and reading the resulting "rpg cd" output, adding a safety overhead for lag, then saving per-command milliseconds and updating UI inputs.

Sequence (applied per command)
1) Send the action command.
2) Wait 2 seconds.
3) Send "rpg cd".
4) Wait 1 second + extra 1 second (render/lag buffer).
5) Parse the cooldowns message and read the remaining time for that command.
6) Add an overhead of 2 + 1 + 1 = 4 seconds to the remaining time and persist (milliseconds).
7) Update UI textbox for commands that have one (Hunt, Work, Farm).
8) Wait 3 seconds before moving to the next command.

Commands initialized
- Hunt: action "rpg hunt h" → persists hunt_ms, updates HuntCdBox
- Adventure: action "rpg adventure" → persists adventure_ms (no textbox)
- Farm: action "rpg farm" → persists farm_ms, updates FarmCdBox
- Work: action "rpg chainsaw" → persists work_ms, updates WorkCdBox

Persistence
- File: %LocalAppData%/EpicRPGBot.UI/settings/cooldowns.ini
- Keys (milliseconds): 
  - hunt_ms=
  - adventure_ms=
  - work_ms=
  - farm_ms=
- On app load (UI), if present:
  - HuntCdBox uses hunt_ms (default: 61,000 ms)
  - WorkCdBox uses work_ms (default: current UI value if not found)
  - FarmCdBox uses farm_ms (default: current UI value if not found)

Notes
- Visual cooldown labels remain driven by parsing the "rpg cooldowns" message and a 1s ticking timer; Inicialize establishes persisted baselines used by inputs and engine scheduling.
- Sending uses WebView2 DevTools (Input.insertText + Enter keyDown/keyUp) with a fallback to execCommand to guarantee actual message submission.