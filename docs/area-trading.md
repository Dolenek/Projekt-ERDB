# Area Trading

`EpicRPGBot.UI` includes a one-click `Trade area` sweep on the right-side control panel.

Supported areas:
- `3`
- `5`
- `7`
- `8`
- `9`
- `10`
- `11`
- `15`

UI behavior:
- The `Trade area` button sits under `rpg cd`.
- Clicking it starts immediately and writes progress to the Console log.
- The button is disabled while the sweep is running.
- No separate modal window is used in v1.

Workflow behavior:
- The sweep starts with `rpg p` and parses `Area: ... (Max: X)` from the EPIC RPG profile reply.
- The saved configured area is updated to that live max area when it changes.
- If the live area is unsupported, the sweep stops without sending trade or dismantle steps.
- If the engine is running, the app pauses it, waits for the send lane to go idle, runs the sweep exclusively, then resumes the engine and refreshes with `rpg cd`.

Configured plans:
- Area `3`: dismantle `banana` all, dismantle `ultra log` all, trade `C all`, trade `B all`
- Area `5`: dismantle `ultra log` all, dismantle `epic fish` all, trade `E all`, trade `A all`, trade `D all`
- Area `7`: dismantle `banana` all, trade `C all`
- Area `8`: dismantle `ultra log` all, dismantle `epic fish` all, trade `E all`, trade `A all`, trade `D all`
- Area `9`: dismantle `mega log` all, dismantle `banana` all, trade `E all`, trade `C all`, trade `B all`
- Area `10`: dismantle `banana` all, trade `C all`
- Area `11`: trade `E all`
- Area `15`: dismantle `epic fish` all, dismantle `banana` all, trade `E all`, trade `A all`, trade `C all`

Failure rules:
- Dismantle-all steps reuse the normal dismantling workflow and keep skipping empty tiers on the way down.
- If a dismantle-all step still ends with an unrecognized or failed reply, the sweep logs that dismantle failure and continues into the later trade steps.
- Trade steps ignore failure replies and continue to the next trade step.
- If EPIC RPG rejects the `all` amount for a trade step, the sweep logs that trade failure and moves on.
- If EPIC RPG replies with the standard `wait at least 1s` cooldown warning for a trade step, the sweep waits 1 second and resends the same trade command.
- Missing confirmed replies for the initial `rpg p` refresh and unparsable profile replies still stop the sweep.

Automation IDs:
- Launcher: `TradeAreaButton`
