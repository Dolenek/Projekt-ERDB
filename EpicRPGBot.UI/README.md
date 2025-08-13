# EpicRPGBot.UI

A WPF (.NET Framework 4.8) desktop UI that embeds Discord via WebView2. The center shows the live Discord channel, the left sidebar now provides three tabs (Last messages, Stats, Console), and the right panel contains bot settings and controls. The bot automation is integrated directly in this app using WebView2 DevTools (no separate Chrome/Selenium instance).

- Center: Embedded Discord chat (WebView2)
- Left: Tabs
  - Last messages: Rolling last 5 messages from the active channel
  - Stats: Counters such as “Hunt sent: N”
  - Console: Structured logs (including “Message (… ) sent” entries)
- Right: Settings and Start/Stop controls

Key behavior
- Start Bot immediately sends “rpg cd”, then continues with hunt/work/farm sequence and timers.
- Commands are sent via WebView2 DevTools to the real message composer (not the header search).
- Console logs every bot message send as: Message (command) sent
- The “Last messages” tab shows the most recent 5 chat lines with timestamps.
- The “Stats” tab shows an incrementing counter of “rpg hunt…” commands sent.

Project structure (UI-layer)
- [EpicRPGBot.UI/EpicRPGBot.UI.csproj](EpicRPGBot.UI/EpicRPGBot.UI.csproj:1)
- [EpicRPGBot.UI/App.xaml](EpicRPGBot.UI/App.xaml:1)
- [EpicRPGBot.UI/App.xaml.cs](EpicRPGBot.UI/App.xaml.cs:1)
- [EpicRPGBot.UI/MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1)
- [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1)
- Engine:
  - [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1)
- Models:
  - [EpicRPGBot.UI/Models/MessageItem.cs](EpicRPGBot.UI/Models/MessageItem.cs:1)
  - [EpicRPGBot.UI/Models/LogEntry.cs](EpicRPGBot.UI/Models/LogEntry.cs:1)
- Services:
  - [EpicRPGBot.UI/Services/InMemoryLog.cs](EpicRPGBot.UI/Services/InMemoryLog.cs:1)
  - [EpicRPGBot.UI/Services/LastMessagesBuffer.cs](EpicRPGBot.UI/Services/LastMessagesBuffer.cs:1)
  - [EpicRPGBot.UI/Services/UiDispatcher.cs](EpicRPGBot.UI/Services/UiDispatcher.cs:1)
- Env loader (shared style with original console app):
  - [EpicRPGBot.UI/Env.cs](EpicRPGBot.UI/Env.cs:1)

Requirements
- .NET Framework 4.8 Developer Pack
- WebView2 Evergreen Runtime (installed system-wide)
- Windows 10/11 x64

Install WebView2 Runtime (if needed)
- Using winget:
  winget install --id Microsoft.EdgeWebView2Runtime --exact --accept-package-agreements --accept-source-agreements

Environment configuration (.env)
At repository root, create a .env file (copy from .env.example) and set the following keys as needed:
- DISCORD_CHANNEL_URL=https://discord.com/channels/yourGuildId/yourChannelId
- AREA=10
- HUNT_COOLDOWN=21000
- WORK_COOLDOWN=99000
- FARM_COOLDOWN=196000

Build and run
- Build the whole solution:
  dotnet build EpicRPGBotCSharp.sln -c Debug
- Run the UI project:
  dotnet run --project EpicRPGBot.UI -c Debug

Usage
1) Launch the app and log into Discord in the center pane if not already authenticated. The app keeps a persistent WebView2 user data folder.
2) Enter or confirm the Discord Channel URL (right panel). The “Go To Channel” button navigates directly there.
3) Adjust area/cooldowns if needed.
4) Click “Start Bot”:
   - Immediately sends “rpg cd”
   - Then sends hunt/work/farm opening salvo based on your area
   - Continues on timers
5) Left sidebar tabs:
   - Last messages: Shows rolling last 5 messages from the active channel with timestamps
   - Stats: Shows counters such as “Hunt sent: N” (increments whenever a command starting with “rpg hunt” is sent)
   - Console: Shows structured log lines when commands are sent or the engine starts/stops (including “Message (… ) sent”)
6) Click “Stop Bot” to stop timers and pause the engine.

Notes on behavior
- Message composition: The engine focuses the bottom message composer and sends text using DevTools Input.insertText, then presses Enter by dispatching DevTools key events. This avoids accidentally typing into the header search box.
- Start behavior: The app always sends “rpg cd” once on Start, and logs this to Console. If you also press Start again quickly, duplicate engine instances are prevented and sending respects a simple delay guard.
- Event scanning: The engine polls the DOM to capture the last chat message and triggers bot reactions for certain keywords. Reaction messages are also emitted through the same event path so they are logged as “Message (… ) sent”.

Files of interest
- UI layout: [EpicRPGBot.UI/MainWindow.xaml](EpicRPGBot.UI/MainWindow.xaml:1)
- UI logic / event wiring: [EpicRPGBot.UI/MainWindow.xaml.cs](EpicRPGBot.UI/MainWindow.xaml.cs:1)
- Bot automation: [EpicRPGBot.UI/BotEngine.cs](EpicRPGBot.UI/BotEngine.cs:1)
- Logging and buffers:
  - [EpicRPGBot.UI/Services/InMemoryLog.cs](EpicRPGBot.UI/Services/InMemoryLog.cs:1)
  - [EpicRPGBot.UI/Services/LastMessagesBuffer.cs](EpicRPGBot.UI/Services/LastMessagesBuffer.cs:1)
  - [EpicRPGBot.UI/Models/LogEntry.cs](EpicRPGBot.UI/Models/LogEntry.cs:1)
  - [EpicRPGBot.UI/Models/MessageItem.cs](EpicRPGBot.UI/Models/MessageItem.cs:1)

Troubleshooting
- WebView2 init failed:
  - Ensure “Microsoft Edge WebView2 Runtime” is installed (see command above).
- Types into header “Search” instead of composer:
  - Use the latest build. The engine focuses the composer using a bottom-most visible role="textbox" heuristic. If Discord updates break selectors, rebuild from main; the engine contains a fallback path via execCommand.
- Discord interstitial prompts (“Open/Continue in Browser”):
  - The app auto-clicks common interstitials after navigation. If you still see them, click manually once and they won’t reappear often.

Security and compliance
- This UI drives the Discord web client via WebView2. Only use in your own account and in allowed channels. Respect Discord’s Terms of Service and bot/game rules.

Changelog (UI integration highlights)
- Added three-tab left sidebar: Last messages, Stats, Console
- Renamed the old “Stats” (last 5 messages) to “Last messages”
- Added “Stats” tab with “Hunt sent: N” counter (counts commands starting with “rpg hunt”)
- Console now logs every bot send as “Message (… ) sent”
- Immediate “rpg cd” on Start, DevTools-based input to composer; avoids header search box