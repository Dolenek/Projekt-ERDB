# Guild Raid Watcher

`EpicRPGBot.UI` includes a dedicated guild-raid watcher that runs independently from `Start Bot`.

Runtime model:
- The watcher uses a separate `Guild tab` WebView2 surface.
- It starts when the app finishes loading and stays active while the app is open.
- `Start Bot` and `Stop Bot` do not start or stop the guild watcher.
- The watcher navigates only when both the guild channel URL and trigger text are configured.

Match behavior:
- The watched message body is matched against the saved trigger text.
- Match mode can be `contains` or `exact`.
- Matching is case-insensitive.
- Exact matching trims both values before comparison.
- An optional author filter requires the Discord author/app text to contain the configured value.
- One Discord message id can trigger at most once.

Send behavior:
- When a new matching message appears, the watcher sends `rpg guild raid`.
- The command is sent through the same guild tab and same channel being watched.
- After sending `rpg guild raid`, the watcher temporarily suppresses further trigger sends while it waits for either an EPIC GUARD prompt or the raid result message from EPIC RPG.
- If EPIC GUARD appears on the guild tab during that watch window, the app logs it, switches to the Guild tab, and shows the same desktop guard alert used by the main bot tab.
- The temporary guard watch ends when the raid result message is seen.
- If settings become incomplete or the guild URL is invalid, the watcher goes idle without affecting the main bot engine.

Settings:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- Keys: `guild_raid_channel_url`, `guild_raid_trigger_text`, `guild_raid_match_mode`, `guild_raid_author_filter`
- The main settings window opens a dedicated `Guild raid` dialog for these values.
- Changes save immediately and apply live to the watcher.

Automation IDs:
- Main settings launcher: `SettingsGuildRaidButton`
- Dialog root: `GuildRaidSettingsWindow`
- Dialog fields: `GuildRaidSettingsChannelUrlInput`, `GuildRaidSettingsTriggerInput`, `GuildRaidSettingsMatchModeInput`, `GuildRaidSettingsAuthorFilterInput`
- Dialog action: `GuildRaidSettingsCloseButton`
