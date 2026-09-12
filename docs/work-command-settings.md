# Work Command Settings

`EpicRPGBot.UI` stores work-command text as a per-area map for areas `1` through `15`.

Behavior:
- Each area has one saved work command text, editable directly by the user.
- The `Work commands` button in the settings window opens a modal editor for all 15 areas.
- Changes save immediately to the shared settings snapshot and the local `.ini` file.
- `Auto-Best` is a one-shot action that replaces all 15 commands from current EPIC RPG data.
- `Start Bot` resolves the current work command from the saved area and the saved per-area map.
- `Initialize` uses that same resolved work command for its work baseline discovery step.
- A running engine receives the newly resolved command for the configured area immediately.
- Saved work commands are normalized to include the `rpg ` prefix when missing.

Auto-Best inputs:
- `rpg p` supplies time travels plus the wallet and bank balances; coin decisions use their sum.
- With the saved `Ascended` setting enabled, `rpg pr` supplies the Worker profession level.
- For Worker level 100 or higher, `rpg boost` identifies active Fish and Wood potions.
- Profile parsing failure leaves every saved command unchanged.
- Missing Worker data or Worker below level 100 falls back to regular recommendations.
- Missing boost data uses the ascended table without both potions.
- The second ascended table requires both potions; one potion uses the first table.

Regular recommendations:
- Areas `1-2`: `chop`; areas `3-5`: `axe`.
- Areas `6-7`: `pickaxe` below 20m total coins or at TT 2 and below; otherwise `ladder`.
- Area `8`: the same condition selects `pickaxe`; otherwise `bowsaw`.
- Area `9`: `pickaxe` below 20m total coins; otherwise `chainsaw`.
- Areas `10-11`: `drill` below 30m total coins; otherwise `chainsaw`.
- Areas `12-15`: `dynamite` below 30m total coins; otherwise `chainsaw`.
- The 20m and 30m boundaries belong to the `otherwise` branch.

Ascended recommendations:
- The saved `Ascended` switch selects this behavior; Auto-Best does not infer it from profile text.

| Potions | Worker | A1-3 | A4-5 | A6-7 | A8 | A9 | A10+ |
|---|---:|---|---|---|---|---|---|
| Not both | 100-101 | dynamite | dynamite | dynamite | dynamite | dynamite | chainsaw |
| Not both | 102-106 | dynamite | dynamite | greenhouse | dynamite | dynamite | chainsaw |
| Not both | 107-108 | dynamite | dynamite | greenhouse | dynamite | greenhouse | chainsaw |
| Not both | 109-114 | dynamite | dynamite | greenhouse | chainsaw | greenhouse | chainsaw |
| Not both | 115+ | dynamite | chainsaw | greenhouse | chainsaw | greenhouse | chainsaw |
| Both | 100-108 | dynamite | dynamite | dynamite | dynamite | dynamite | chainsaw |
| Both | 109-113 | dynamite | dynamite | greenhouse | dynamite | dynamite | chainsaw |
| Both | 114-117 | dynamite | dynamite | greenhouse | dynamite | greenhouse | chainsaw |
| Both | 118-122 | dynamite | dynamite | greenhouse | chainsaw | greenhouse | chainsaw |
| Both | 123+ | dynamite | chainsaw | greenhouse | chainsaw | greenhouse | chainsaw |

Defaults:
- Areas `1-2` default to `rpg chop`.
- Areas `3-5` default to `rpg axe`.
- Areas `6-8` default to `rpg bowsaw`.
- Areas `9-15` default to `rpg chainsaw`.

Persistence:
- Stored in `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`.
- Key: `work_commands`
- Format: semicolon-separated `area=command` pairs covering all areas `1..15`.
- Auto-Best uses the same format and commits its complete map with one settings save.

Validation:
- Empty values fall back to the default command for that area.
- Areas outside `1..15` are clamped to the supported range before lookup.
