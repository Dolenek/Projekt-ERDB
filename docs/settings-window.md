# Settings Window

`EpicRPGBot.UI` exposes user-adjustable values in a dedicated modal settings window opened from the main header `Settings` button.

Layout:
- `Discord` section: channel URL and the `Fallback to @me if empty` toggle
- `Bot Parameters` section: area plus hunt/adventure/work/farm/lootbox cooldown baselines
- `Close` button only; there is no separate save/apply action

Persistence model:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- The settings window loads from the shared in-memory settings snapshot when it opens.
- Every edit saves immediately back to the shared snapshot and the backing `.ini` file.
- Existing keys are preserved: `channel_url`, `use_at_me_fallback`, `area`, `hunt_ms`, `adventure_ms`, `work_ms`, `farm_ms`, `lootbox_ms`

Runtime behavior:
- `Go To Channel` reads the current saved settings snapshot, not a textbox on the main window.
- `Start Bot` reads area and cooldown baselines from the same shared snapshot.
- `Inicialize` updates the saved hunt/adventure/work/farm/lootbox baseline values through the shared settings service after parsing refreshed cooldowns.
- Closing and reopening the settings window always shows the latest persisted values.

Automation IDs:
- Main window launcher: `SettingsButton`
- Dialog root: `SettingsWindow`
- Dialog fields: `SettingsChannelUrlInput`, `SettingsUseAtMeFallback`, `SettingsAreaInput`
- Cooldown fields: `SettingsHuntCooldownInput`, `SettingsAdventureCooldownInput`, `SettingsWorkCooldownInput`, `SettingsFarmCooldownInput`, `SettingsLootboxCooldownInput`
- Dialog action: `SettingsCloseButton`
