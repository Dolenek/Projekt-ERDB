# Guard Operations

The EPIC GUARD flow now tracks the Discord message id for each detected guard prompt.

Behavior:
- The engine starts at most one solve attempt per guard message id.
- If the same prompt message is seen again while its solve is active, the duplicate trigger is ignored.
- If the same prompt message is seen again after it was already handled, the duplicate trigger is ignored.
- When the guard clear message appears, the active solve is cancelled and the active guard message id is cleared.

Console visibility:
- Solver progress is written with the existing `[solver]` prefix.
- Typical lines include:
  - guard detection source,
  - duplicate-trigger suppression,
  - solve start for a specific message id,
  - image capture source,
  - model submission,
  - chosen answer,
  - whether the answer was sent to chat.

Chat send behavior:
- While a guard solve is active, scheduled tracked commands and queued cooldown snapshot sends are skipped so they do not occupy the Discord send lane.
- While a guard incident is active, other event-triggered fast replies such as `CUT`, `LURE`, `CATCH`, or coin/NPC reactions are suppressed until the clear message arrives.
- When the model returns a valid catalog match, the bot sends that exact item name back to Discord.
- If the solver is uncertain or the send fails, the console logs that outcome instead of silently doing nothing.
- If the guard clear message also contains the delayed result of a tracked command such as `farm`, that same message still counts as the tracked command response so scheduling resumes normally after the clear.
- After the clear message is seen, the engine also queues one fresh `rpg cd` snapshot so tracked timers are resynced from current EPIC RPG state.

Classification behavior:
- The solver uses color as a strong signal when the captcha is clearly colored.
- Grayscale notes are treated as fallback guidance for black-and-white or desaturated captchas.
- Catalog disambiguation notes can explicitly rule out near-duplicate items such as `banana` versus `unicorn horn`.

Console copy:
- The Console list supports `Ctrl+C`.
- If one or more console lines are selected, `Ctrl+C` copies their rendered log lines to the clipboard.
