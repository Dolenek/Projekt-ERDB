# Command Send Confirmation

`rpg ...` commands now use a two-step confirmation path before the next command is allowed through the bot send lane.

Confirmation rules:
- Step 1: Discord must show the outgoing command as a new chat message.
- Step 2: A newer message from the `EPIC RPG` author must appear after that outgoing command.
- The send lane stays blocked until both steps succeed or the retry budget is exhausted.

Scope:
- Applies to real `rpg ...` commands only.
- Used by `Start Bot`, tracked hunt/adventure/work/farm/lootbox sends, queued/manual `rpg cd`, and the `Inicialize` workflow.
- Does not apply to quick-time/event answers such as `RUN`, `CUT`, `LURE`, `CATCH`, `SUMMON`, `TIME TO FIGHT`, `I WANT THAT`, or similar prompt responses.
- Does not apply to bot status/help/acknowledgement text such as `I am farming` or `Change work - ...`.

Retries and timing:
- Outgoing registration is polled for up to about 4 seconds.
- EPIC RPG reply confirmation is polled for up to 10 seconds after each registered send.
- The bot retries a failed or unconfirmed `rpg ...` command up to 3 total attempts.
- Retries wait 1 second between attempts.
- The existing global 1-second gap between bot-originated sends is still enforced.

Runtime effects:
- Tracked command timers still re-arm from the EPIC RPG confirmation message, not from the local send timestamp.
- For tracked hunt/adventure/work/farm/lootbox sends, the scheduler marks the command as pending as soon as the outgoing `rpg ...` message is visible.
- If all confirmation attempts fail, the caller keeps its existing failure handling, such as retry scheduling for tracked commands.
