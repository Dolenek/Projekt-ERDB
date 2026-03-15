# Cooldown Management

The UI has two cooldown concepts: persisted scheduling baselines and visual countdown labels.

Persisted baselines:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- Keys currently used:
  - `hunt_ms`
  - `adventure_ms`
  - `work_ms`
  - `farm_ms`
- On app load, `hunt_ms`, `adventure_ms`, `work_ms`, and `farm_ms` are restored directly into the settings textboxes.
- These values are the baseline cooldowns used after a successful EPIC RPG confirmation, not fixed local send intervals.

Visual cooldown labels:
- The right panel shows parsed cooldowns for rewards, experience, and progress commands.
- The tracker watches the most recent Discord message.
- When a message contains `cooldowns`, it parses entries such as `quest | epic quest (1m 2s)` and maps aliases to canonical labels.
- A 1-second UI timer decrements any active label until it reaches `Ready`.

Runtime scheduling:
- Hunt, adventure, work, and farm are rescheduled when EPIC RPG answers the command.
- If EPIC RPG replies with `wait at least ...`, the engine retries after that reported remaining time plus a small safety buffer.
- This avoids early retries caused by Discord/network delay between local send time and server-side command registration.
- When the bot sends hunt/adventure/work/farm, the matching right-side cooldown label is armed immediately from the configured textbox value.

Alias mapping preserved in the current app:
- `quest` and `epic quest` map to the same label.
- `chop`, `fish`, `pickup`, and `mine` map to the work label.
- `horse breeding` and `horse race` map to the horse label.
- `dungeon` and `miniboss` map to the dungeon label.

`Inicialize` workflow:
1. Send `rpg hunt h`, wait 2 seconds, send `rpg cd`, wait 2 more seconds, parse cooldowns, then persist `hunt_ms`.
2. Repeat the same pattern for `rpg adventure`, `rpg farm`, and `rpg chainsaw`.
3. Add a fixed 4-second safety overhead to the parsed remaining time before saving.
4. Update the Hunt, Adventure, Work, and Farm textboxes with the saved milliseconds.

Defaults used when no persisted value exists:
- Hunt: `61000`
- Work: current textbox value or `99000`
- Farm: current textbox value or `196000`
