# Automation Engine

`EpicRPGBot.UI/BotEngine.cs` is the runtime orchestrator for periodic command sending and chat-triggered reactions. In the two-tab shell it always targets the bot Discord tab, never the player tab.

Engine lifecycle:
- `Start()` uses the saved work command for the current area, starts message polling, and waits for the startup `rpg cd` snapshot before scheduling hunt/adventure/training/work/farm/lootbox.
- `Stop()` stops hunt/adventure/training/work/farm/lootbox/message timers and marks the engine as stopped.
- `SendImmediateAsync()` is used by the UI to send the initial `rpg cd` after startup polling is armed.
- `QueueCooldownSnapshotRequest()` coalesces manual `rpg cd` refresh requests into one pending send while the engine is running.

Timer behavior:
- Hunt/adventure/training/work/farm/lootbox timers are one-shot timers that are re-armed after the EPIC RPG reply is seen.
- Startup does not send the opening command burst anymore; it schedules each command from the parsed `rpg cd` remaining time and sends immediately only if that command is ready.
- A successful reply starts the next timer from the confirmation time, not from the local send time.
- A reply containing `wait at least ...` schedules a retry using the reported remaining time plus a small buffer.
- The engine processes recent unseen Discord messages in order, so event posts are not skipped when a normal command result lands right after them.
- Adventure uses `rpg adv h`.
- Training uses `rpg tr`.
- Work uses the saved per-area command text for the current area.
- Farm runs when the saved area is `>= 4`, or earlier if `Ascended` is enabled in settings.
- Lootbox uses `rpg buy ed lb`.
- Message timer polls the last Discord message every 2 seconds and runs the reaction rules.

Command send behavior:
- Commands are sent through the bot tab chat client.
- All bot-originated sends share one global send lane with a 1-second gap between commands.
- `rpg ...` commands are not considered complete when the composer clears; the bot waits for the outgoing command to appear in chat and then for a newer reply authored by `EPIC RPG` before the next command can enter the lane.
- Real command sends retry up to 3 times when outgoing registration or the EPIC RPG reply is missing, and later `rpg ...` commands stay blocked behind that retry loop.
- Quick-time/event prompt answers, including training prompt answers, and bot status/help text still use the fast path and do not wait for an EPIC RPG follow-up reply.
- The right-side cooldown panel starts hunt/adventure/training/work/farm/lootbox when the EPIC RPG confirmation reply is received, without waiting for the next `rpg cd`.
- When a confirmed `rpg tr` reply is a recognized training prompt, the engine resolves the answer from the rendered message body, prefers clicking the matching button, and falls back to typing.
- If a training prompt cannot be solved safely, the engine skips it, logs a `[training]` warning through the UI, and raises a desktop notification.
- When the UI parses a fresh `rpg cd` snapshot or a time-cookie reduction, the engine resyncs hunt/adventure/training/work/farm/lootbox timers from the tracked cooldown panel and clears stale pending replies.
- Successful sends raise `OnCommandSent`, which is what drives the Console view and the hunt counter.
- Confirmed `rpg ...` replies raise `OnCommandConfirmed`, which is what drives tracked cooldown starts in the UI.

Reaction behavior currently implemented:
- `TEST` replies with a timestamp.
- `BOT HELP` prints the work/farm help lines.
- `STOP` stops timers.
- `START` sends `rpg cd`, then waits for the cooldown snapshot and reinitializes scheduling from it.
- `CHANGE WORK ...` switches the work command and acknowledges the change.
- `CHANGE FARM ...` switches the farm command and acknowledges the change.
- `BOT FARMING` starts farm-only sending if farm is not already running.
- Event phrases such as zombie horde, megarace boost, epic tree, megalodon, raining coins, NPC trade, lootbox summoning, and legendary boss keep their existing one-line responses.
- EPIC GUARD incidents are tracked as an active alert state: the first detection shows a full alert, reminders are rate-limited to once every 10 seconds, and the incident clears when the `Everything seems fine ... keep playing` message is seen.
- That clear message also resumes tracked timers immediately and cancels the active captcha solve attempt.

Special cases:
- Coin and NPC prompts respond with the first matching option already present in the message text.
- Some event replies are sent directly and intentionally do not raise `OnCommandSent`, matching the current behavior.
- Guard/captcha detection still pauses timers, attempts the solve flow, and resumes timers afterward; the first alert still switches back to the bot tab before showing the notification if the player tab was selected.
