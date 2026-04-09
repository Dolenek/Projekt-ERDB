# Sleepy Potion

`EpicRPGBot.UI` includes a `Sleepy potion` button in the `Time cookie` section, directly under `Dungeon`.

Current behavior:
- Clicking the button starts a one-shot exclusive workflow.
- While active, the button label changes to `Stop Sleepy potion`.
- Clicking the active button again cancels the in-flight workflow.

Command sequence:
1. Ensure the normal automation engine is running.
2. Send `rpg cd`.
3. Wait for currently ready automated tracked commands to finish.
4. Send `rpg egg use sleepy potion`.
5. Send `rpg cd` again.
6. Wait for any newly-ready automated tracked commands to finish.
7. Stop the workflow.

Engine interaction:
- If the engine was already running, it stays running during and after the workflow.
- If the engine was stopped, the app starts it for the workflow and stops it again when the workflow ends.
- While the workflow is active, other exclusive bot-producing actions are blocked by the shared exclusive-operation gate.

Cooldown handling:
- The workflow does not try to infer a fixed 1-day reduction from the sleepy-potion reply text.
- The second `rpg cd` snapshot is the authoritative cooldown refresh after the potion is used.
- The same tracked scheduler logic used by `Time cookie` is reused so automated daily/weekly/hunt/adventure/training/work/farm/lootbox sends can drain before and after the item use.

Logging:
- The Console logs each major step with the `[sleepy potion]` prefix.
