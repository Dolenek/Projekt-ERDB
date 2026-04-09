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
- After the outgoing `rpg ...` message becomes visible, the sender waits 500 ms before it starts looking for the EPIC RPG reply.
- EPIC RPG reply confirmation is polled for up to 10 seconds after each registered send.
- The bot retries a failed or unconfirmed `rpg ...` command up to 3 total attempts.
- Retries wait 1 second between attempts.
- The existing global 1-second gap between bot-originated sends is still enforced.
- Before the send lane presses `Enter`, it now dispatches `Escape` once to close Discord mention/autocomplete popovers so raw text targets such as `rpg dung @handle` can send as plain text instead of getting stuck in the composer.

Runtime effects:
- Tracked command timers still re-arm from the EPIC RPG confirmation message, not from the local send timestamp.
- The confirmed-send path now feeds the EPIC RPG reply snapshot directly into the tracked scheduler, so a tracked command re-arms even if the background poller processes that reply later.
- The right-side tracked cooldown panel also starts hunt/adventure/work/farm/lootbox from confirmation receipt, not from outgoing registration.
- For tracked hunt/adventure/work/farm/lootbox sends, the scheduler marks the command as pending as soon as the outgoing `rpg ...` message is visible.
- If all confirmation attempts fail, the caller keeps its existing failure handling, such as retry scheduling for tracked commands.
- Confirmed send results now retain the full EPIC RPG reply snapshot text, which higher-level workflows such as crafting, dismantling, and area trading use for reply parsing.
- The fallback EPIC RPG reply detector recognizes profile, craft, dismantle, and trade-style replies when Discord does not expose the author or exact outgoing message id cleanly enough.
- When a confirmed reply snapshot is found, the engine processes that exact snapshot immediately before relying on the next recent-message poll. This prevents interactive replies such as training prompts from being missed by a follow-up poll race.
- `Stop Bot` cancels in-flight send confirmation waits and retries instead of waiting for the full timeout budget.
