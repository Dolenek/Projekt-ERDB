# Guild Raid Watcher

`EpicRPGBot.UI` includes a dedicated guild-raid watcher that runs independently from `Start Bot`.

Runtime model:
- The watcher uses a separate `Guild tab` WebView2 surface.
- It starts only when the Guild Raid `Active` setting is enabled and the saved configuration is valid.
- The Guild WebView2 is disposed when the watcher is inactive and the Guild tab is not selected.
- `Start Bot` and `Stop Bot` do not start or stop the guild watcher.
- Enabling or disabling the watcher applies immediately without restarting the app.

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
- After sending `rpg guild raid`, the watcher temporarily suppresses further trigger sends while it waits for either a quiz challenge prompt or the raid result message from EPIC RPG.
- If a quiz challenge appears on the guild tab during that watch window, the app logs it, switches to the Guild tab, and shows the same desktop guard alert used by the main bot tab.
- The temporary guard watch ends when the raid result message is seen.
- If the watcher is disabled, settings become incomplete, or the guild URL is invalid, polling stops and the watcher goes idle without affecting the main bot engine.

Settings:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- Keys: `guild_raid_active`, `guild_raid_channel_url`, `guild_raid_trigger_text`, `guild_raid_match_mode`, `guild_raid_author_filter`
- The main settings window opens a dedicated `Guild raid` dialog for these values.
- Changes save immediately and apply live to the watcher.
- `Active` defaults to `false`, including when upgrading from settings that do not yet contain `guild_raid_active`.

Automation IDs:
- Main settings launcher: `SettingsGuildRaidButton`
- Dialog root: `GuildRaidSettingsWindow`
- Dialog fields: `GuildRaidSettingsActiveInput`, `GuildRaidSettingsChannelUrlInput`, `GuildRaidSettingsTriggerInput`, `GuildRaidSettingsMatchModeInput`, `GuildRaidSettingsAuthorFilterInput`
- Dialog action: `GuildRaidSettingsCloseButton`
