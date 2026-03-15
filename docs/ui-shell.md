# UI Shell

The active application is `EpicRPGBot.UI`, a WPF `.NET Framework 4.8` desktop app with three fixed columns.

Layout:
- Left sidebar: tabs for `Last messages`, `Stats`, and `Console`.
- Center pane: embedded Discord web client through WebView2.
- Right pane: channel URL, area/cooldown inputs, `Start Bot`, `Stop Bot`, `Inicialize`, and visual cooldown labels.

Startup flow:
1. Load captcha-only `.env` values into process environment for solver configuration.
2. Load saved local settings from `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini` into the right-side inputs.
3. Bind the last-message list and in-memory console log.
4. Restore the last saved channel URL, fallback flag, area, and hunt/work/farm values.
5. Initialize WebView2 with a persistent profile under `%LocalAppData%/EpicRPGBot.UI/WebView2`.
6. Navigate to the saved channel URL, or `https://discord.com/channels/@me` if the URL is empty and fallback is enabled.
7. Start polling Discord for the last visible message every 2 seconds.

User-visible behaviors:
- `Reload` reloads the embedded Discord tab.
- `Go To Channel` navigates the embedded browser to the URL in the settings box.
- `Start Bot` immediately sends `rpg cd`, then starts the automation engine.
- `Stop Bot` stops engine timers but keeps the UI running.
- `Inicialize` runs the cooldown discovery workflow described in [cooldown-management](cooldown-management.md).
- Any change to the channel URL, fallback flag, area, or hunt/work/farm boxes is saved immediately to local settings.

Sidebar data:
- `Last messages` shows the rolling last 5 detected channel messages with timestamps.
- `Stats` currently shows `Hunt sent: N`.
- `Console` shows structured log lines for UI events, engine events, sent commands, and solver telemetry.

Browser behavior:
- The app enables WebView2 devtools, zoom controls, and default context menus.
- On navigation completion it auto-clicks common Discord interstitials such as `Continue in browser`.
- Message sending is targeted at the bottom Discord composer, not the header search box.
