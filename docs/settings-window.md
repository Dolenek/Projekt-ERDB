# Settings Window

`EpicRPGBot.UI` exposes user-adjustable values in a dedicated modal settings window opened from the main header `Settings` button.

Layout:
- The dialog reuses the main window dark palette, control styling, and custom resizable title bar.
- Its initial size is `900×680`, it clamps itself inside the current Windows work area, and smaller heights use vertical scrolling without horizontal overflow.
- `Work commands`, `Guild raid`, and `Card Hand` use the same responsive shell, fixed footer, auto-save badge, window controls, and work-area clamping.
- The advanced dialogs show their complete content at the default size; reduced heights use only a dark vertical scrollbar.
- Left column: `Discord` channel and dungeon-listing URLs, the `Fallback to @me if empty` toggle, and launchers for focused advanced dialogs.
- Right column: area, `Ascended`, dungeon-channel cleanup, EPIC GUARD foreground behavior, and hunt/adventure/training/work/farm/lootbox cooldown baselines.
- `Close` button only; there is no separate save/apply action
- `Work commands` opens a second modal with editable work-command text rows for areas `1..15` and a one-shot `Auto-Best` profile loader
- `Guild raid` opens a second modal with guild-raid channel, trigger text, match mode, and optional author filter
- `Card Hand` opens a second modal for auto-play, nine reward weights, and the persisted deck-ownership import; see [Card hand automation](card-hand-automation.md)

Persistence model:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- The settings window loads from the shared in-memory settings snapshot when it opens.
- Every edit saves immediately back to the shared snapshot and the backing `.ini` file.
- Existing keys are preserved. Card-hand keys store the enabled flag, nine weights, deck-loaded flag, sorted owned-card codes, and successful-load UTC timestamp.
- `Bring the EPIC GUARD tab and app to the foreground` is disabled by default. It controls tab switching and window activation together without disabling guard sound or system notifications.

Runtime behavior:
- `Go to channel` reads the current saved settings snapshot, not a textbox on the main window.
- `Start` reads area and cooldown baselines from the same shared snapshot.
- `Start` also resolves the work command from the saved area and the saved per-area work map.
- Work-command edits and successful Auto-Best runs update the running engine for the configured area immediately.
- Auto-Best shows an indeterminate progress indicator while its confirmed Discord queries are running, supports cancellation when the dialog closes, and only applies a complete 15-area result.
- `Initialize` updates the saved hunt/adventure/training/work/farm/lootbox baseline values through the shared settings service after parsing refreshed cooldowns.
- `Initialize` also refreshes the cached profile player name through the shared settings service after parsing `rpg p`.
- The guild-raid dialog updates the always-on guild watcher live through the same shared settings service.
- Guard solving and submission continue in the background when foreground presentation is disabled. Enabling it makes the first detection select the relevant Discord tab and activate the app window.
- Closing and reopening the settings window always shows the latest persisted values.

Automation IDs:
- Main window launcher: `SettingsButton`
- Dialog root: `SettingsWindow`
- Dialog fields: `SettingsChannelUrlInput`, `SettingsDungeonListingChannelUrlInput`, `SettingsUseAtMeFallback`, `SettingsAreaInput`, `SettingsAscendedInput`, `SettingsAutoDeleteDungeonChannelInput`, `SettingsBringGuardAlertsToForegroundInput`
- Cooldown fields: `SettingsHuntCooldownInput`, `SettingsAdventureCooldownInput`, `SettingsTrainingCooldownInput`, `SettingsWorkCooldownInput`, `SettingsFarmCooldownInput`, `SettingsLootboxCooldownInput`
- Dialog actions: `SettingsWorkCommandsButton`, `SettingsGuildRaidButton`, `SettingsCardHandButton`, `SettingsCloseButton`
- Window chrome: `SettingsMinimizeButton`, `SettingsMaximizeRestoreButton`, `SettingsWindowCloseButton`
- Card-hand dialog: `CardHandSettingsWindow`, `CardHandAutoPlayInput`, `CardHandLoadDeckButton`, `CardHandDeckStatus`, `CardHandWeight{Reward}`, `CardHandSettingsCloseButton`, `CardHandSettingsMinimizeButton`, `CardHandSettingsMaximizeRestoreButton`, `CardHandSettingsWindowCloseButton`
- Guild-raid dialog: `GuildRaidSettingsWindow`, `GuildRaidSettingsChannelUrlInput`, `GuildRaidSettingsTriggerInput`, `GuildRaidSettingsMatchModeInput`, `GuildRaidSettingsAuthorFilterInput`, `GuildRaidSettingsCloseButton`, `GuildRaidSettingsMinimizeButton`, `GuildRaidSettingsMaximizeRestoreButton`, `GuildRaidSettingsWindowCloseButton`
- Work-commands dialog: `WorkCommandsWindow`, `WorkCommandsAutoBestButton`, `WorkCommandsAutoBestStatus`, `WorkCommandsCloseButton`, `WorkCommandsMinimizeButton`, `WorkCommandsMaximizeRestoreButton`, `WorkCommandsWindowCloseButton`, and `WorkCommandArea{N}Input` for areas `1..15`
