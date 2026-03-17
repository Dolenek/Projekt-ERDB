# Work Command Settings

`EpicRPGBot.UI` stores work-command text as a per-area map for areas `1` through `15`.

Behavior:
- Each area has one saved work command text, editable directly by the user.
- The `Work commands` button in the settings window opens a modal editor for all 15 areas.
- Changes save immediately to the shared settings snapshot and the local `.ini` file.
- `Start Bot` resolves the current work command from the saved area and the saved per-area map.
- `Inicialize` uses that same resolved work command for its work baseline discovery step.
- Saved work commands are normalized to include the `rpg ` prefix when missing.

Defaults:
- Areas `1-2` default to `rpg chop`.
- Areas `3-5` default to `rpg axe`.
- Areas `6-8` default to `rpg bowsaw`.
- Areas `9-15` default to `rpg chainsaw`.

Persistence:
- Stored in `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`.
- Key: `work_commands`
- Format: semicolon-separated `area=command` pairs covering all areas `1..15`.

Validation:
- Empty values fall back to the default command for that area.
- Areas outside `1..15` are clamped to the supported range before lookup.
