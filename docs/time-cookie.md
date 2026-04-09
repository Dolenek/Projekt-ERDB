# Time Cookie

`EpicRPGBot.UI` includes a `Time cookie` section under `Bot Controls` with `Dungeon`, `Duel`, and `Card hand` buttons.

Current behavior:
- Clicking one button starts an exclusive loop for that cooldown target.
- The active button label changes to `Stop <Target>`.
- The other time-cookie buttons are disabled while a loop is active.
- Clicking the active button again stops the loop.

Loop rules:
- The loop makes sure the normal automation engine is running while it works.
- At the start of each cycle it refreshes cooldowns with `rpg cd`.
- It lets the normal tracked automation batch finish first, including any training prompt handling.
- If the selected target cooldown is already `Ready`, the loop stops and leaves that target unused for the player.
- Otherwise it sends `rpg use time cookie`.
- After the time-cookie reduction is parsed, the full cooldown panel visual is reduced immediately and then any newly-ready automated tracked commands are allowed to finish before the next cycle.
- The loop stops instead of guessing when `rpg cd` fails, `rpg use time cookie` fails, or the reply does not contain a recognized `X minute(s) ahead` reduction.

Targets:
- `Dungeon` watches the `dungeon` / `miniboss` cooldown row.
- `Duel` watches the `duel` cooldown row.
- `Card hand` watches the `card hand` cooldown row.

Engine interaction:
- If the normal engine was already running when the loop starts, it stays running during and after the workflow.
- If the engine was stopped when the loop starts, the app starts it for the workflow and stops it again when the workflow ends.
- While the loop is active, other bot-producing UI actions such as `Start Bot`, `Stop Bot`, `Inicialize`, `rpg cd`, `Trade area`, `Crafting`, `Dismantle`, and `Wishing token` are ignored.

Logging:
- The Console logs each major step with the `[time cookie]` prefix.
