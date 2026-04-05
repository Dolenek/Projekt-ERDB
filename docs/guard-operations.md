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
- When the model returns a valid catalog match, the bot sends that exact item name back to Discord.
- If the solver is uncertain or the send fails, the console logs that outcome instead of silently doing nothing.

Console copy:
- The Console list supports `Ctrl+C`.
- If one or more console lines are selected, `Ctrl+C` copies their rendered log lines to the clipboard.
