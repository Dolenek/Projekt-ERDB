# UI Shell

The active application is `EpicRPGBot.UI`, a WPF `.NET Framework 4.8` desktop app with three fixed columns.

Layout:
- Left sidebar: tabs for `Last messages`, `Stats`, and `Console`.
- Center pane: a `TabControl` with `Bot tab` and `Player tab`, each hosting its own Discord WebView2 surface, plus a header button row with `Settings`, `Reload`, and `Go To Channel`.
- Right pane: the `Start Bot` / `Stop Bot` / `Inicialize` / `rpg cd` controls at the top and the visual cooldown labels anchored to the bottom.

Startup flow:
1. Load captcha-only `.env` values into process environment for solver configuration.
2. Load saved local settings from `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini` into an in-memory settings snapshot.
3. Bind the last-message list and in-memory console log.
4. Use the saved channel URL, fallback flag, area, and hunt/adventure/work/farm/lootbox baselines as the runtime defaults for navigation and automation.
5. Warm both Discord tabs once during startup so each WebView2 surface is realized before the user switches tabs.
6. Initialize both WebView2 tabs with a shared persistent profile under `%LocalAppData%/EpicRPGBot.UI/WebView2`.
7. Navigate both tabs to the saved channel URL, or `https://discord.com/channels/@me` if the URL is empty and fallback is enabled.
8. Start polling the bot tab for the last visible message every 2 seconds.

User-visible behaviors:
- `Dismantle` opens a modal dismantling window for log, fish, and banana dismantle requests.
- `Crafting` opens a modal crafting window for log, fish, and banana craft requests.
- `Settings` opens a modal settings window with the editable Discord channel and bot-parameter fields.
- `Reload` reloads the bot tab even if the player tab is currently selected.
- `Go To Channel` navigates only the bot tab to the currently saved channel URL.
- `Start Bot` starts the automation engine, then sends `rpg cd` through the bot tab and waits for the cooldown snapshot before scheduling commands.
- `Stop Bot` stops engine timers but keeps both tabs open.
- `Inicialize` starts with one opening `rpg cd` snapshot, skips tracked commands that are already on cooldown in that snapshot, and only saves refreshed baselines for commands that were ready to initialize.
- `rpg cd` queues one cooldown refresh at the next legal bot send slot while the engine is running, or sends immediately through the bot tab when the engine is stopped.
- `Trade area` starts a one-click live-area dismantle/trade sweep and logs progress to the Console.
- The button grid keeps `Start Bot`, `Stop Bot`, and `Inicialize` on the first row, with `rpg cd` below `Start Bot`, `Trade area` beside it, and one remaining slot unused.
- If the engine is running when crafting starts, the UI pauses the engine, waits for the current send lane to go idle, runs the craft job exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when dismantling starts, the UI pauses the engine, waits for the current send lane to go idle, runs the dismantle job exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when area trading starts, the UI pauses the engine, waits for the current send lane to go idle, runs the area-trade sweep exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- Any change in the settings window is saved immediately to local settings.
- When the UI sees an EPIC RPG profile message containing `Area: ... (Max: X)`, it updates the saved configured area to `X`.
- If the bot detects the EPIC GUARD captcha while the player tab is selected, the UI switches back to the bot tab and shows the existing alert.
- The last-message/cooldown pipeline deduplicates Discord messages by message id so snapshots and time-cookie reductions are not applied twice.

Sidebar data:
- `Last messages` shows the rolling last 5 detected channel messages with timestamps.
- `Stats` shows send counts for `hunt`, `adventure`, `work`, `farm`, and `lootbox`, plus live running-cooldown counts for all tracked cooldown rows, including totals for `Rewards`, `Experience`, and `Progress`.
- `Console` shows structured log lines for UI events, engine events, sent commands, and solver telemetry.

Browser behavior:
- The app enables WebView2 devtools, zoom controls, and default context menus.
- On navigation completion it auto-clicks common Discord interstitials such as `Continue in browser`.
- Message sending, polling, and captcha solving are targeted at the bot tab composer, not the player tab.
- The player tab is manual-only and is not used by automation.
