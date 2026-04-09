# Pet Automation

`EpicRPGBot.UI` uses the same fast catch planner for the post-training pet prompts that appear after `rpg tr`.

Runtime behavior:
- Cat, dog, and dragon pet prompts are handled by the same reactive path that already answers bunny prompts.
- Pet handling is always enabled during normal engine automation.
- The engine recognizes pet prompts from the rendered Discord body when it contains the pet footer `Use "info" to get information about pets`, a supported `... TIER IS APPROACHING` line, and the `Happiness` / `Hunger` stat labels.
- Pet replies use the fast raw-send path, not the confirmed `rpg ...` command loop.
- While a pet reply is pending, the global send lane stays blocked so scheduled commands do not race the event answer.
- The interactive prompt lock clears immediately after the reply send attempt finishes; v1 does not wait for a result message.

Reply planning:
- The parser reads numeric `Happiness` and `Hunger` values from the rendered Discord message body.
- The planner reuses the bunny catch strategy exactly: evaluate every `feed` / `pat` split up to 6 actions, prefer the shortest guaranteed catch plan, and otherwise use the best 6-action heuristic.
- Replies are emitted with all `feed` actions first, followed by all `pat` actions.

Fallback and alerts:
- If the prompt is recognized but the numeric values are unreadable, the bot sends the fixed fallback reply `feed feed feed pat pat pat`.
- Fallback sends and failures are logged with the `[pet]` prefix and raise a desktop balloon titled `Pet prompt issue`.
