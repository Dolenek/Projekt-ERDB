# Cooldown Management

The UI has two cooldown layers: persisted baselines and the live tracked cooldown state shown in the right panel.

Persisted baselines:
- File: `%LocalAppData%/EpicRPGBot.UI/settings/app-settings.ini`
- Keys currently used: `hunt_ms`, `adventure_ms`, `training_ms`, `work_ms`, `farm_ms`, `lootbox_ms`
- These values are loaded into the shared settings snapshot on app load and shown in the settings window when it opens.
- They are fallback/base durations for tracked commands, not the authoritative live state once `rpg cd` has been parsed.

Tracked cooldown state:
- The right panel shows parsed cooldowns for rewards, experience, and progress commands.
- When a message contains `cooldowns`, the tracker parses entries such as `quest | epic quest (1m 2s)` and maps aliases to canonical labels.
- A 1-second UI timer decrements active labels until they reach `Ready`.
- The Stats sidebar also shows live counts for currently running cooldowns across all tracked rows and per section (`Rewards`, `Experience`, `Progress`).
- Rows that are `Ready` get a stable green/light-green background based on fixed row order; active cooldown rows keep the default dark background.
- For hunt, adventure, training, work, farm, and lootbox, the tracked values are also used to resync the runtime scheduler after a fresh `rpg cd` snapshot.

Time-cookie handling:
- EPIC RPG replies containing `time cookie` plus `X minute(s) ahead` reduce the tracked cooldown state immediately.
- The reduction is applied to tracked hunt/adventure/training/work/farm/lootbox values in the right panel and floors expired timers to `Ready`.
- Time-cookie detection does not auto-send `rpg cd`.
- The dedicated `Time cookie` workflow also watches the untracked `dungeon`, `duel`, or `card hand` row chosen by the user and stops when that selected row becomes `Ready`, without auto-using that target command.

Runtime scheduling:
- The bot still arms hunt/adventure/training/work/farm/lootbox labels immediately when it sends those commands, using the configured settings baseline.
- If EPIC RPG replies with `wait at least ...`, the engine retries after that reported remaining time plus a small safety buffer.
- After a parsed `rpg cd` snapshot or time-cookie reduction, hunt/adventure/training/work/farm/lootbox scheduling is resynced from the tracked cooldown state.
- The `Time cookie` workflow reuses that same tracked scheduling so normal automated commands can finish before and after each `rpg use time cookie`.
- Incoming Discord messages are deduplicated by message id so cooldown snapshots and time-cookie reductions are only applied once.

Alias mapping preserved in the current app:
- `quest` and `epic quest` map to the same label.
- `chop`, `fish`, `pickup`, and `mine` map to the work label.
- `horse breeding` and `horse race` map to the horse label.
- `dungeon` and `miniboss` map to the dungeon label.

`Inicialize` workflow:
1. Send one opening `rpg cd` and parse it before any command-specific initialization starts.
2. If hunt, adventure, training, work, farm, or lootbox already has remaining time in that opening snapshot, skip that command and leave its saved setting unchanged.
3. For commands that were ready in the opening snapshot, send the command, wait 2 seconds, send `rpg cd`, parse the refreshed remaining time, and persist the corresponding `*_ms` value.
4. Add a fixed 4-second safety overhead to the parsed remaining time before saving.
5. The next settings-window open reflects the saved milliseconds only for commands that were actually initialized.

Defaults used when no persisted value exists:
- Hunt: `61000`
- Training: `61000`
- Work: `99000`
- Farm: `196000`
- Lootbox: `21600000`
