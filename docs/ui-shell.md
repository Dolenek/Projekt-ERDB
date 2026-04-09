# UI Shell

The active application is `EpicRPGBot.UI`, a WPF `.NET Framework 4.8` desktop app with three fixed columns.

Layout:
- Left sidebar: tabs for `Last messages`, `Stats`, and `Console`.
- Center pane: a `TabControl` with `Player tab`, `Bot tab`, `Guild tab`, and `Dungeon tab`, each hosting its own Discord WebView2 surface, plus a header button row with `Settings`, `Reload`, and `Go To Channel`.
- Right pane: the `Start Bot` / `Stop Bot` / `Inicialize` / `rpg cd` / `Trade area` / `Wishing token` / `Complete Dungeon` controls at the top, a `Time cookie` target row under them, and the visual cooldown labels anchored to the bottom.

Startup flow:
1. Load captcha-only `.env` values into process environment for solver configuration.
2. Load saved local settings from `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini` into an in-memory settings snapshot.
3. Bind the last-message list and in-memory console log.
4. Use the saved channel URL, fallback flag, area, and hunt/adventure/work/farm/lootbox baselines as the runtime defaults for navigation and automation.
5. Warm all four Discord tabs once during startup so each WebView2 surface is realized before the user switches tabs.
6. Initialize all four WebView2 tabs with a shared persistent profile under `%LocalAppData%/EpicRPGBot.UI/WebView2`.
7. Navigate the bot, player, and dungeon tabs to the saved bot channel URL, or `https://discord.com/channels/@me` if the URL is empty and fallback is enabled.
8. If guild-raid settings are complete, navigate the guild tab to the saved guild-raid channel URL.
9. Start polling the bot tab for the last visible message every 2 seconds.
10. Start the guild watcher so it can monitor the guild tab even while the main bot engine is stopped.

User-visible behaviors:
- `Dismantle` opens a modal dismantling window for log, fish, and banana dismantle requests.
- `Crafting` opens a modal crafting window for log, fish, and banana craft requests.
- `Settings` opens a modal settings window with the editable Discord channel, `Ascended`, per-area work-command access, and bot-parameter fields.
- `Settings` also exposes a `Guild raid` launcher that opens the dedicated guild-raid settings dialog.
- `Reload` reloads the bot tab even if the player tab is currently selected.
- `Go To Channel` navigates only the bot tab to the currently saved channel URL.
- The browser tabs open with `Player tab` selected by default, and the currently selected tab is highlighted light blue.
- `Complete Dungeon` starts an exclusive run that first performs `Trade area` on the bot tab in its currently open channel, then switches to the dedicated `Dungeon tab` for the listing-channel signup; while active, the button changes to `Stop Dungeon`.
- `Start Bot` starts the automation engine, then sends `rpg cd` through the bot tab and waits for the cooldown snapshot before scheduling commands.
- `Stop Bot` stops engine timers but keeps all four tabs open.
- While the engine is running, `Start Bot` is tinted light green and `Stop Bot` stays white; while the engine is stopped, `Stop Bot` is tinted light red and `Start Bot` stays white.
- `Inicialize` starts with one opening `rpg cd` snapshot, skips tracked commands that are already on cooldown in that snapshot, and only saves refreshed baselines for commands that were ready to initialize.
- `rpg cd` queues one cooldown refresh at the next legal bot send slot while the engine is running, or sends immediately through the bot tab when the engine is stopped.
- `Trade area` starts a one-click live-area dismantle/trade sweep and logs progress to the Console.
- `Wishing token` starts an exclusive `rpg use wishing token` loop that keeps selecting `time cookie` until the user clicks the same button again or the workflow stops on an unrecognized state.
- The button grid keeps `Start Bot`, `Stop Bot`, and `Inicialize` on the first row, with `rpg cd`, `Trade area`, and `Wishing token` on the second row, and `Complete Dungeon` spanning the full third row.
- The `Time cookie` section sits under that grid and exposes `Dungeon`, `Duel`, and `Card hand` target buttons.
- `Time cookie` starts an exclusive loop that refreshes `rpg cd`, lets normal tracked automation finish, uses `rpg use time cookie`, then waits for newly-ready tracked commands to finish until the selected target cooldown reaches `Ready`.
- If the engine is running when crafting starts, the UI pauses the engine, waits for the current send lane to go idle, runs the craft job exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when dismantling starts, the UI pauses the engine, waits for the current send lane to go idle, runs the dismantle job exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when area trading starts, the UI pauses the engine, waits for the current send lane to go idle, runs the area-trade sweep exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when `Wishing token` starts, the UI pauses the engine, runs the loop exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when `Complete Dungeon` starts, the UI pauses the engine, runs the mandatory pre-dungeon area trade on the bot tab, continues the dungeon workflow on the dungeon tab, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is stopped when `Time cookie` starts, the UI starts it for the workflow and stops it again when the workflow ends; if it was already running, it keeps running throughout the workflow.
- Any change in the settings window is saved immediately to local settings.
- The `Work commands` settings modal also auto-saves each per-area command change immediately.
- The guild watcher stays active while the app is open and sends `rpg guild raid` from the guild tab when a watched message matches the configured rule.
- When the UI sees an EPIC RPG profile message containing `Area: ... (Max: X)`, it updates the saved configured area to `X`.
- `Inicialize` also refreshes the cached profile player name from `rpg p`.
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
- The guild tab is used only by the guild-raid watcher and its `rpg guild raid` sends.
- The dungeon tab is used only by the `Complete Dungeon` workflow.
