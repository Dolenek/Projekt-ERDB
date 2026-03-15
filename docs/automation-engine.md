# Automation Engine

`EpicRPGBot.UI/BotEngine.cs` is the runtime orchestrator for periodic command sending and chat-triggered reactions.

Engine lifecycle:
- `Start()` sets the work command from the selected area, starts message polling, and waits for the startup `rpg cd` snapshot before scheduling hunt/adventure/work/farm.
- `Stop()` stops hunt/adventure/work/farm/message timers and marks the engine as stopped.
- `SendImmediateAsync()` is used by the UI to send the initial `rpg cd` before `Start()`.

Timer behavior:
- Hunt/adventure/work/farm timers are one-shot timers that are re-armed after the EPIC RPG reply is seen.
- Startup does not send the opening command burst anymore; it schedules each command from the parsed `rpg cd` remaining time and sends immediately only if that command is ready.
- A successful reply starts the next timer from the confirmation time, not from the local send time.
- A reply containing `wait at least ...` schedules a retry using the reported remaining time plus a small buffer.
- The engine processes recent unseen Discord messages in order, so event posts are not skipped when a normal command result lands right after them.
- Adventure uses `rpg adv h`.
- Work still uses `rpg chop`, `rpg axe`, `rpg bowsaw`, or `rpg chainsaw` based on the selected area.
- Farm still runs only when `area >= 4`.
- Message timer polls the last Discord message every 2 seconds and runs the reaction rules.

Command send behavior:
- Commands are sent through the shared Discord chat client.
- All bot-originated sends share one global send lane with a 1-second gap between commands.
- The chat client waits for Discord to submit or clear the composer before the next queued command can be sent.
- The right-side cooldown panel is also updated immediately when the bot sends hunt/adventure/work/farm, without waiting for the next `rpg cd`.
- Successful sends raise `OnCommandSent`, which is what drives the Console view and the hunt counter.

Reaction behavior currently implemented:
- `TEST` replies with a timestamp.
- `BOT HELP` prints the work/farm help lines.
- `STOP` stops timers.
- `START` sends `rpg cd`, then waits for the cooldown snapshot and reinitializes scheduling from it.
- `CHANGE WORK ...` switches the work command and acknowledges the change.
- `CHANGE FARM ...` switches the farm command and acknowledges the change.
- `BOT FARMING` starts farm-only sending if farm is not already running.
- Event phrases such as zombie horde, megarace boost, epic tree, megalodon, raining coins, NPC trade, lootbox summoning, and legendary boss keep their existing one-line responses.

Special cases:
- Coin and NPC prompts respond with the first matching option already present in the message text.
- Some event replies are sent directly and intentionally do not raise `OnCommandSent`, matching the current behavior.
