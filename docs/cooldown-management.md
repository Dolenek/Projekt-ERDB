# Cooldown Management

The UI has two cooldown layers: persisted baselines and the live tracked cooldown state shown in the right panel.

Persisted baselines:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- Keys currently used: `hunt_ms`, `adventure_ms`, `work_ms`, `farm_ms`, `lootbox_ms`
- These values are restored into the settings textboxes on app load.
- They are fallback/base durations for tracked commands, not the authoritative live state once `rpg cd` has been parsed.

Tracked cooldown state:
- The right panel shows parsed cooldowns for rewards, experience, and progress commands.
- When a message contains `cooldowns`, the tracker parses entries such as `quest | epic quest (1m 2s)` and maps aliases to canonical labels.
- A 1-second UI timer decrements active labels until they reach `Ready`.
- For hunt, adventure, work, farm, and lootbox, the tracked values are also used to resync the runtime scheduler after a fresh `rpg cd` snapshot.

Time-cookie handling:
- EPIC RPG replies containing `time cookie` plus `X minute(s) ahead` reduce the tracked cooldown state immediately.
- The reduction is applied to tracked hunt/adventure/work/farm/lootbox values in the right panel and floors expired timers to `Ready`.
- Time-cookie detection does not auto-send `rpg cd`.

Runtime scheduling:
- The bot still arms hunt/adventure/work/farm/lootbox labels immediately when it sends those commands, using the configured textbox baseline.
- If EPIC RPG replies with `wait at least ...`, the engine retries after that reported remaining time plus a small safety buffer.
- After a parsed `rpg cd` snapshot or time-cookie reduction, hunt/adventure/work/farm/lootbox scheduling is resynced from the tracked cooldown state.
- Incoming Discord messages are deduplicated by message id so cooldown snapshots and time-cookie reductions are only applied once.

Alias mapping preserved in the current app:
- `quest` and `epic quest` map to the same label.
- `chop`, `fish`, `pickup`, and `mine` map to the work label.
- `horse breeding` and `horse race` map to the horse label.
- `dungeon` and `miniboss` map to the dungeon label.

`Inicialize` workflow:
1. Send `rpg hunt h`, wait 2 seconds, send `rpg cd`, wait 2 more seconds, parse cooldowns, then persist `hunt_ms`.
2. Repeat the same pattern for `rpg adv h`, `rpg farm`, `rpg chainsaw`, and `rpg buy ed lb`.
3. Add a fixed 4-second safety overhead to the parsed remaining time before saving.
4. Update the Hunt, Adventure, Work, Farm, and Lootbox textboxes with the saved milliseconds.

Defaults used when no persisted value exists:
- Hunt: `61000`
- Work: current textbox value or `99000`
- Farm: current textbox value or `196000`
- Lootbox: current textbox value or `21600000`
