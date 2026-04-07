# Dungeon Automation

`EpicRPGBot.UI` includes a one-click `Complete Dungeon` workflow on the right-side control panel.

UI behavior:
- The workflow runs on the dedicated `Dungeon tab`.
- The `Complete Dungeon` button toggles to `Stop Dungeon` while a run is active.
- Starting the workflow selects the `Dungeon tab` and logs progress to the Console.
- If the normal engine is running, the app pauses it for the dungeon run and resumes it with `rpg cd` afterward.

Profile identity:
- The app caches the player name parsed from the EPIC RPG `rpg p` profile header.
- `Inicialize` refreshes that cached player name on the bot tab after the cooldown setup sequence finishes.
- If the cached name is empty when a dungeon run starts, the dungeon workflow refreshes it with `rpg p` before continuing.

Workflow behavior:
1. Navigate the `Dungeon tab` to the saved bot channel URL.
2. Send `rpg p` in the dungeon signup channel.
3. Navigate to Discord `@me`, open the fixed `Army Helper` DM, and wait up to 15 minutes for a newer message with `Take me there`.
4. Click the newest `Take me there` button.
5. In the opened dungeon channel, parse the `Players listed` message, resolve the non-self Discord mention, and send `rpg dung <@partnerId>`.
6. Click `yes` on the entry prompt.
7. During battle, whenever the latest encounter state says it is the cached player’s turn, send `bite`.
8. Stop when the recent dungeon messages confirm a win, failure, cancellation, or timeout.

Battle rules:
- The workflow treats `ALL PLAYERS WON` and the final `Thanks for using our dungeon system` message as successful completion.
- The workflow treats `ALL PLAYERS LOST`, `GAME OVER`, and `dungeon failed` as failure states.
- If the battle state does not change for 60 seconds after the encounter starts, the run stops.

Settings:
- `Auto delete dungeon channel after a win` controls whether the workflow clicks the red `Delete dungeon channel` button after a successful run.
- The cached profile name is persisted in settings but is not user-editable.

Automation IDs:
- Launcher: `CompleteDungeonButton`
- Browser tab: `DungeonBrowserTab`
- Browser surface: `DungeonDiscordWebView`
