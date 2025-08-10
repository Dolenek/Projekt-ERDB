UI integration

Summary
This PR adds a new WPF (.NET Framework 4.8) UI that embeds Discord via WebView2 and moves the bot automation directly into the UI (no Selenium/Chrome instance). It also replaces the root README with an English, UI‑focused guide and adds per‑file documentation for the UI and console components.

Highlights
- New WPF UI project EpicRPGBot.UI (WebView2-based)
- In-app automation engine using WebView2 DevTools (Input.insertText + key events)
- Reliable composer focus (avoids typing into the Discord header search)
- Start Bot now:
  - Immediately sends “rpg cd”
  - Then continues with hunt/work/farm opening salvo based on Area
  - Timers continue sending commands on cooldown
- Left sidebar with two tabs:
  - Stats: rolling last 5 messages (with timestamps)
  - Console: structured logs (engine start/stop, commands sent)
- Persistent WebView2 profile for Discord login
- Clicks common interstitials automatically after navigation
- README overhaul and English documentation for each code file

What’s included
- EpicRPGBot.UI/
  - App.xaml, App.xaml.cs
  - MainWindow.xaml, MainWindow.xaml.cs
  - BotEngine.cs (WebView2 automation + timers + event hooks)
  - Env.cs (simple .env loader)
  - Services: InMemoryLog, LastMessagesBuffer, UiDispatcher
  - README.md (UI usage, build and troubleshooting)
  - docs/ (per-file docs)
    - App.xaml.md
    - EpicRPGBot.UI.csproj.md
    - MainWindow.xaml.md
    - MainWindow.xaml.cs.md
    - BotEngine.cs.md
    - InMemoryLog.cs.md
    - LastMessagesBuffer.cs.md
    - UiDispatcher.cs.md
    - Env.cs.md
- EpicRPGBotCSHARP/docs/Program.cs.md (console bot reference)
- Root README.md replaced with English UI-focused README

Behavioral notes
- Composer focus: Engine picks the bottom-most visible role="textbox" (or data-slate-editor) element and focuses it before sending text via DevTools; falls back to execCommand if needed.
- Start dedupe: Clicking Start while running is ignored (no duplicate ‘rpg cd’ and no double timers).
- Message polling: The UI polls last message text and records into a rolling buffer for the Stats tab.

How to run
- Build: dotnet build EpicRPGBotCSharp.sln -c Debug
- Run UI: dotnet run --project EpicRPGBot.UI -c Debug
- Ensure WebView2 Runtime is installed (winget install Microsoft.EdgeWebView2Runtime)
- Configure .env with DISCORD_CHANNEL_URL and cooldowns (see README)

Screenshots
- Not included yet; structure allows adding under EpicRPGBot.UI/docs/img and referencing them in README.

Risks / follow-ups
- Discord DOM can change; selectors are resilient but may require updates.
- Future: expose engine events to the left panels directly (currently basic logging/polling is in place).
- Optional: add more command customization and richer stats.

License
- MIT (project default).