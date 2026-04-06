# Settings Window

`EpicRPGBot.UI` exposes user-adjustable values in a dedicated modal settings window opened from the main header `Settings` button.

Layout:
- `Discord` section: channel URL and the `Fallback to @me if empty` toggle
- `Bot Parameters` section: area, `Ascended`, a `Work commands` button, and hunt/adventure/training/work/farm/lootbox cooldown baselines
- `Close` button only; there is no separate save/apply action
- `Work commands` opens a second modal with editable work-command text rows for areas `1..15`

Persistence model:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- The settings window loads from the shared in-memory settings snapshot when it opens.
- Every edit saves immediately back to the shared snapshot and the backing `.ini` file.
- Existing keys are preserved and extended with `ascended`, `training_ms`, and `work_commands`.

Runtime behavior:
- `Go To Channel` reads the current saved settings snapshot, not a textbox on the main window.
- `Start Bot` reads area and cooldown baselines from the same shared snapshot.
- `Start Bot` also resolves the work command from the saved area and the saved per-area work map.
- `Inicialize` updates the saved hunt/adventure/training/work/farm/lootbox baseline values through the shared settings service after parsing refreshed cooldowns.
- Closing and reopening the settings window always shows the latest persisted values.

Automation IDs:
- Main window launcher: `SettingsButton`
- Dialog root: `SettingsWindow`
- Dialog fields: `SettingsChannelUrlInput`, `SettingsUseAtMeFallback`, `SettingsAreaInput`, `SettingsAscendedInput`
- Cooldown fields: `SettingsHuntCooldownInput`, `SettingsAdventureCooldownInput`, `SettingsTrainingCooldownInput`, `SettingsWorkCooldownInput`, `SettingsFarmCooldownInput`, `SettingsLootboxCooldownInput`
- Dialog actions: `SettingsWorkCommandsButton`, `SettingsCloseButton`
- Work-commands dialog: `WorkCommandsWindow`, `WorkCommandsCloseButton`, and `WorkCommandArea{N}Input` for areas `1..15`
