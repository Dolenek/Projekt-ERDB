# UI Shell

The active application is `EpicRPGBot.UI`, a WPF `.NET Framework 4.8` desktop app with a custom dark window shell and three resizable-content columns.

Layout:
- The custom title bar exposes the packaged app icon, Discord and bot status, `Start`, `Stop`, `Reload`, `Settings`, and standard minimize/maximize/close controls. The window remains draggable and resizable through WPF `WindowChrome`; maximization is constrained to the current monitor work area so it never extends underneath the Windows taskbar.
- Left `Activity` sidebar: `Messages`, `Stats`, and `Console` views with case-insensitive search; Console also supports a structured log-kind filter.
- Center pane: `Player`, `Bot`, `Guild`, `Dungeon`, and `Duel` tabs, each hosting its own Discord WebView2 surface. `Go to channel` remains beside the browser tabs.
- Right `Control Center`: compact grouped quick actions, inventory tools, run workflows, time-cookie controls, and the complete visual cooldown list without a nested scrollbar.
- The Activity and Control Center sidebars start at 300 and 360 pixels and can independently collapse to a 36-pixel reopening rail. Collapse state is not persisted.

Startup flow:
1. Clamp the initial normal window to the current monitor work area with a DPI-aware margin so it cannot open beyond any screen edge.
2. Load puzzle-only `.env` values into process environment for solver configuration.
3. Load saved local settings from `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini` into an in-memory settings snapshot.
4. Bind the last-message list and in-memory console log.
5. Use the saved channel URL, fallback flag, area, and hunt/adventure/work/farm/lootbox baselines as the runtime defaults for navigation and automation.
6. Warm all five Discord tabs once during startup so each WebView2 surface is realized before the user switches tabs.
7. Initialize all five WebView2 tabs with a shared persistent profile under `%LocalAppData%/EpicRPGBot.UI/WebView2`.
8. Navigate the bot, player, and dungeon tabs to the saved bot channel URL, navigate the duel tab to its fixed `dueling-2` channel, and use `https://discord.com/channels/@me` as the configured-channel fallback.
9. If guild-raid settings are complete, navigate the guild tab to the saved guild-raid channel URL.
10. Start polling the bot tab for the last visible message every 2 seconds.
11. Start the guild watcher so it can monitor the guild tab even while the main bot engine is stopped.

User-visible behaviors:
- `Pets` opens the [pet menu](pet-menu.md) with inventory selection, protections and manual/automatic fusion. The bot remains paused while the modal menu is open.
- `Dismantle` opens a modal dismantling window for log, fish, and banana dismantle requests.
- `Crafting` opens a modal crafting window for log, fish, and banana craft requests.
- `Settings` opens a modal settings window with the editable Discord channel, `Ascended`, per-area work-command access, and bot-parameter fields.
- `Settings` also exposes a `Guild raid` launcher that opens the dedicated guild-raid settings dialog.
- `Reload` reloads the bot tab even if the player tab is currently selected.
- `Go to channel` navigates only the bot tab to the currently saved channel URL.
- The browser tabs open with `Player` selected by default, and the current tab uses a cyan underline.
- `Complete dungeon` starts an exclusive run that first performs `Trade area` on the bot tab in its currently open channel, then switches to the dedicated Dungeon tab for the listing-channel signup; while active, the button changes to `Stop dungeon`.
- `Start duel` loads the profile through the channel currently held by the Bot tab and runs matchmaking in the dedicated Duel tab without forcing it into the foreground; while active, the button changes to `Stop duel`.
- `Start` starts the automation engine, then sends `rpg cd` through the bot tab and waits for the cooldown snapshot before scheduling commands.
- `Stop` stops engine timers but keeps all five tabs open.
- The title bar shows `Stopped`, `Running`, or the active exclusive workflow. Discord status progresses through `Initializing`, `Ready`, or `Error`.
- `Start` uses the green primary treatment, `Stop` uses a restrained red treatment, and active exclusive workflow buttons use the cyan treatment.
- `Initialize` starts with one opening `rpg cd` snapshot, skips tracked commands that are already on cooldown in that snapshot, and only saves refreshed baselines for commands that were ready to initialize.
- `rpg cd` queues one cooldown refresh at the next legal bot send slot while the engine is running, or sends immediately through the bot tab when the engine is stopped.
- `Trade area` starts a one-click live-area dismantle/trade sweep and logs progress to the Console.
- `Wishing token` starts an exclusive `rpg use wishing token` loop that keeps selecting `time cookie` until the user clicks the same button again or the workflow stops on an unrecognized state.
- `Sleepy potion` starts a one-shot exclusive workflow that sends `rpg cd`, lets ready automated tracked commands finish, uses `rpg egg use sleepy potion`, refreshes with `rpg cd`, and lets newly-ready tracked commands finish.
- `Quick actions` contains `Initialize`, `Cooldowns`, `Trade area`, and `Wishing token`; `Cooldowns` preserves the former `rpg cd` behavior.
- `Inventory tools` contains `Crafting` and `Dismantle`; `Runs` contains `Complete dungeon` and `Start duel`.
- `Time cookie` exposes `Dungeon`, `Duel`, and `Card hand` target buttons, plus `Sleepy potion` below them.
- `Time cookie` starts an exclusive loop that refreshes `rpg cd`, lets normal tracked automation finish, uses `rpg use time cookie`, then waits for newly-ready tracked commands to finish until the selected target cooldown reaches `Ready`.
- If the engine is stopped when `Sleepy potion` starts, the UI starts it for the workflow and stops it again when the workflow ends; if it was already running, it keeps running throughout the workflow.
- If the engine is running when crafting starts, the UI pauses the engine, waits for the current send lane to go idle, runs the craft job exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when dismantling starts, the UI pauses the engine, waits for the current send lane to go idle, runs the dismantle job exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when area trading starts, the UI pauses the engine, waits for the current send lane to go idle, runs the area-trade sweep exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when `Wishing token` starts, the UI pauses the engine, runs the loop exclusively, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is running when `Complete dungeon` starts, the UI pauses the engine, runs the mandatory pre-dungeon area trade on the bot tab, continues the dungeon workflow on the dungeon tab, then resumes the engine and refreshes cooldown scheduling with `rpg cd`.
- If the engine is stopped when `Time cookie` starts, the UI starts it for the workflow and stops it again when the workflow ends; if it was already running, it keeps running throughout the workflow.
- Any change in the settings window is saved immediately to local settings.
- The `Work commands` settings modal also auto-saves each per-area command change immediately.
- The guild watcher stays active while the app is open and sends `rpg guild raid` from the guild tab when a watched message matches the configured rule.
- After a guild-raid send, the watcher keeps the guild tab under a temporary quiz/result watch and blocks further watched sends until one of those replies arrives.
- When the UI sees an EPIC RPG profile message containing `Area: ... (Max: X)`, it updates the saved configured area to `X`.
- `Initialize` also refreshes the cached profile player name from `rpg p`.
- If the bot detects the quiz challenge while the player tab is selected, the UI switches back to the bot tab and shows the existing alert.
- The last-message/cooldown pipeline deduplicates Discord messages by message id so snapshots and time-cookie reductions are not applied twice.

Sidebar data:
- `Messages` shows the rolling last 5 detected channel messages with timestamps.
- `Stats` shows send counts for `hunt`, `adventure`, `work`, `farm`, and `lootbox`, plus live running-cooldown counts for all tracked cooldown rows, including totals for `Rewards`, `Experience`, and `Progress`.
- `Console` shows structured log lines with severity dots for UI events, engine events, sent commands, and solver telemetry. Selected console lines copy their rendered log text to the clipboard with `Ctrl+C`.
- Activity search filters the active Messages or Console collection without changing the underlying five-message buffer or 500-entry log retention. The log-kind selector is available only in Console.

Browser behavior:
- The app enables WebView2 devtools, zoom controls, and default context menus.
- On navigation completion it auto-clicks common Discord interstitials such as `Continue in browser`.
- Message sending, polling, and puzzle solving are targeted at the bot tab composer, not the player tab.
- The player tab is manual-only and is not used by automation.
- The guild tab is used only by the guild-raid watcher and its `rpg guild raid` sends.
- The dungeon tab is used only by the `Complete dungeon` workflow.
- The duel tab is used only by the duel workflow.
