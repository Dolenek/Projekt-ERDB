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
- The `work` row recognizes both the built-in work aliases and the per-area configured work-command texts from settings, so custom commands such as `rpg dynamite` still map to the same `work` visual row.
- A 1-second UI timer decrements active labels until they reach `Ready`.
- The Stats sidebar also shows live counts for currently running cooldowns across all tracked rows and per section (`Rewards`, `Experience`, `Progress`).
- Rows that are `Ready` get a stable green/light-green background based on fixed row order; active cooldown rows keep the default dark background.
- For daily, weekly, hunt, adventure, training, work, farm, and lootbox, the tracked values are also used to resync the runtime scheduler after a fresh `rpg cd` snapshot.

Time-cookie handling:
- EPIC RPG replies containing `time cookie` plus `X minute(s) ahead` reduce the tracked cooldown state immediately.
- The reduction is applied immediately across the full cooldown panel visual, including reward rows and time-cookie target rows such as `dungeon`, `duel`, and `card hand`, and floors expired timers to `Ready`.
- Runtime scheduler resync also includes daily/weekly reward commands.
- Time-cookie detection does not auto-send `rpg cd`.
- The dedicated `Time cookie` workflow also watches the untracked `dungeon`, `duel`, or `card hand` row chosen by the user and stops when that selected row becomes `Ready`, without auto-using that target command.

Sleepy-potion handling:
- The `Sleepy potion` workflow sends `rpg cd`, waits for ready automated tracked commands to finish, uses `rpg egg use sleepy potion`, sends `rpg cd` again, and waits for newly-ready automated tracked commands to finish.
- Unlike `time cookie`, the app does not infer a fixed cooldown reduction directly from the sleepy-potion reply text.
- The follow-up `rpg cd` snapshot is treated as the authoritative cooldown resync after the potion use.

Runtime scheduling:
- The bot still arms daily/weekly labels with fixed EPIC RPG cooldowns and hunt/adventure/training/work/farm/lootbox labels with the configured settings baseline when it sends those commands.
- If EPIC RPG replies with `wait at least ...`, the engine retries after that reported remaining time plus a small safety buffer.
- After a parsed `rpg cd` snapshot or time-cookie reduction, daily/weekly/hunt/adventure/training/work/farm/lootbox scheduling is resynced from the tracked cooldown state.
- The `Time cookie` workflow reuses that same tracked scheduling so normal automated commands can finish before and after each `rpg use time cookie`.
- Incoming Discord messages are deduplicated by message id so cooldown snapshots and time-cookie reductions are only applied once.

Alias mapping preserved in the current app:
- `quest` and `epic quest` map to the same label.
- Built-in work aliases and configured per-area work-command texts map to the work label.
- `horse breeding` and `horse race` map to the horse label.
- `dungeon` and `miniboss` map to the dungeon label.

`Inicialize` workflow:
1. Send one opening `rpg cd` and parse it before any command-specific initialization starts.
2. If hunt, adventure, training, work, farm, or lootbox already has remaining time in that opening snapshot, skip that command and leave its saved setting unchanged. Daily and weekly use fixed cooldown baselines and are not part of `Inicialize`.
3. For commands that were ready in the opening snapshot, send the command, wait 2 seconds, send `rpg cd`, parse the refreshed remaining time, and persist the corresponding `*_ms` value.
4. Add a fixed 4-second safety overhead to the parsed remaining time before saving.
5. The next settings-window open reflects the saved milliseconds only for commands that were actually initialized.

Defaults used when no persisted value exists:
- Hunt: `61000`
- Training: `61000`
- Work: `99000`
- Farm: `196000`
- Lootbox: `21600000`
