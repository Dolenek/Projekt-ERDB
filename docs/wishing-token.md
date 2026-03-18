# Wishing Token

`EpicRPGBot.UI` includes a `Wishing token` toggle button in the right-side control grid.

Current behavior:
- The button sends `rpg use wishing token`, waits for the EPIC RPG wish-selection reply, clicks the `time cookie` choice, then waits for the result reply.
- The selected choice is the component button at row `2`, column `3` inside the wish menu message.
- While the loop is active, the button label changes to `Stop wishing token`.
- Clicking the button again stops the loop.

Loop rules:
- The loop repeats when the result reply matches either:
  - a failed wish with a `time cookie` consolation prize
  - a successful wish that grants `30` `time cookie`
- The loop stops instead of guessing when:
  - the initial EPIC RPG reply is not the wish menu
  - the `time cookie` button cannot be resolved
  - the result reply times out
  - the result reply is not one of the recognized success or failure forms

Engine interaction:
- If the normal bot engine is running when the loop starts, the app pauses it first.
- When the loop stops, the app resumes the engine and refreshes scheduling with `rpg cd`.
- While the loop is active, other bot-producing UI actions such as `Start Bot`, `Inicialize`, `rpg cd`, `Trade area`, `Crafting`, and `Dismantle` are ignored.

Discord interaction:
- The app targets the wish-menu buttons inside the specific EPIC RPG message that returned from `rpg use wishing token`.
- Button targeting uses visible button row/column ordering derived from DOM position, not global page button order.

Logging:
- The Console logs each major step with the `[wishing token]` prefix.
