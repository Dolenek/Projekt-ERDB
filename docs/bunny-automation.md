# Bunny Automation

`EpicRPGBot.UI` can auto-answer the Easter bunny catch prompt while the engine is running.

Runtime behavior:
- Bunny handling is always enabled during normal engine automation.
- The engine only treats a message as a bunny prompt when the rendered body contains the bunny footer (`How to get a bunny` or `rpg egg info bunny`) and bunny stat labels.
- Bunny replies use the fast raw-send path, not the confirmed `rpg ...` command loop.
- While a bunny reply is pending, the global send lane stays blocked so scheduled commands do not race the event answer.
- The bunny lock clears immediately after the reply send attempt finishes; v1 does not wait for a result message.

Reply planning:
- The parser reads numeric `Happiness` and `Hunger` values from the rendered Discord message body.
- The planner evaluates every `feed` / `pat` split up to 6 actions.
- The planner always sends at least 1 action because Discord cannot submit an empty message.
- The primary optimization goal is to maximize the Easter-egg bonus from unused actions.
- If any split guarantees `happiness - hunger >= 85` using the worst-case per-action gains, the bot chooses the shortest guaranteed plan.
- Fewer guaranteed actions means more eggs: 1 action leaves 5 unused commands for 75 eggs, 2 actions leaves 4 for 60 eggs, and so on.
- If no guaranteed plan exists, the bot chooses the best 6-action heuristic from expected gains.
- Replies are emitted with all `feed` actions first, followed by all `pat` actions.

Fallback and alerts:
- If the prompt is recognized but the numeric values are unreadable, the bot sends the fixed fallback reply `feed feed feed pat pat pat`.
- Fallback sends are logged with the `[bunny]` prefix and also raise a desktop balloon titled `Bunny prompt issue`.
- Failed bunny sends also raise a `[bunny]` warning plus the same desktop alert.

Current prompt contract:
- `:heart: Happiness: <number>`
- `:carrot: Hunger: <number>`
- Bunny footer text containing `How to get a bunny` and `rpg egg info bunny`
