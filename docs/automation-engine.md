# Automation Engine

`EpicRPGBot.UI/BotEngine.cs` is the runtime orchestrator for periodic command sending and chat-triggered reactions.

Engine lifecycle:
- `Start()` sets the work command from the selected area, starts timers, and schedules the opening hunt/work/farm sequence.
- `Stop()` stops hunt/work/farm/message timers and marks the engine as stopped.
- `SendImmediateAsync()` is used by the UI to send the initial `rpg cd` before `Start()`.

Timer behavior:
- Hunt timer sends `rpg hunt h`.
- Work timer sends `rpg chop`, `rpg axe`, `rpg bowsaw`, or `rpg chainsaw` based on the selected area.
- Farm timer sends `rpg farm` only when `area >= 4`.
- Message timer polls the last Discord message every 2 seconds and runs the reaction rules.

Command send behavior:
- Commands are sent through the shared Discord chat client.
- A 2-second delay guard is preserved between periodic sends.
- Successful sends raise `OnCommandSent`, which is what drives the Console view and the hunt counter.

Reaction behavior currently implemented:
- `TEST` replies with a timestamp.
- `BOT HELP` prints the work/farm help lines.
- `STOP` stops timers.
- `START` sends `rpg cd`, then reruns the opening sequence.
- `CHANGE WORK ...` switches the work command and acknowledges the change.
- `CHANGE FARM ...` switches the farm command and acknowledges the change.
- `BOT FARMING` starts farm-only sending if farm is not already running.
- Event phrases such as zombie horde, megarace boost, epic tree, megalodon, raining coins, NPC trade, lootbox summoning, and legendary boss keep their existing one-line responses.

Special cases:
- Coin and NPC prompts respond with the first matching option already present in the message text.
- Some event replies are sent directly and intentionally do not raise `OnCommandSent`, matching the current behavior.
