# Dungeon Automation

`EpicRPGBot.UI` includes a one-click `Complete Dungeon` workflow on the right-side control panel.

UI behavior:
- The workflow runs on the dedicated `Dungeon tab`.
- The `Complete Dungeon` button toggles to `Stop Dungeon` while a run is active.
- Starting the workflow selects the `Dungeon tab` and logs progress to the Console.
- If the normal engine is running, the app pauses it for the pre-dungeon trade phase, resumes it while waiting for Army Helper to find a partner, pauses it again when the invite arrives, and resumes it with `rpg cd` after the dungeon run.
- Before the dungeon signup starts, the workflow switches to the `Bot tab` and runs the normal `Trade area` sweep in the channel that is already open there.

Profile identity:
- The app caches the player name parsed from the EPIC RPG `rpg p` profile header.
- `Inicialize` refreshes that cached player name on the bot tab after the cooldown setup sequence finishes.
- If the cached name is empty when a dungeon run starts, the dungeon workflow refreshes it with `rpg p` before continuing.

Workflow behavior:
1. Switch to the `Bot tab` and run `Trade area` in the channel that is already open there.
2. If that required pre-dungeon area-trade phase finds no configured trade plan for the live area, skip trading and continue the dungeon run.
3. If the pre-dungeon area-trade phase fails for any other reason, stop the dungeon run immediately.
4. Navigate the `Dungeon tab` to the saved `Dungeon listing channel URL`, or fall back to the default channel URL when the listing URL is empty.
5. Send `rpg p` in the dungeon listing channel.
6. Resume normal bot automation while waiting for matchmaking, then navigate to Discord `@me`, open the fixed `Army Helper` DM, and wait up to 15 minutes for a newer message with `Take me there`.
7. When the Army Helper invite arrives, pause normal bot automation and click the newest `Take me there` button.
8. If the dungeon channel already shows the EPIC RPG `ARE YOU SURE YOU WANT TO ENTER?` prompt from the partner's invite, click `yes` immediately without sending a new `rpg dung`.
9. Otherwise parse the `Players listed` message and resolve the non-self partner target. Use `rpg dung <@partnerId>` when Discord exposes a real mention id; otherwise send a text mention with `@...`, preferring the plain Discord handle from the left side when it is ASCII-safe and falling back to the right-side Army Helper player tag when the display handle uses special styling or other unsupported characters.
10. If EPIC RPG says one partner is in the middle of a command, wait 5 seconds and retry `rpg dung` up to 2 times.
11. If all 2 retries still hit the same busy-partner reply, stop trying to enter that dungeon and return to waiting for a fresh `Take me there` invite.
12. Click `yes` on the entry prompt.
13. During battle, whenever the latest encounter state says it is the cached player’s turn, send `bite`.
14. Stop when the recent dungeon messages confirm a win, failure, cancellation, or timeout.

Battle rules:
- The workflow treats `ALL PLAYERS WON` and the final `Thanks for using our dungeon system` message as successful completion.
- The workflow treats `ALL PLAYERS LOST`, `GAME OVER`, and `dungeon failed` as failure states.
- If the battle state does not change for 60 seconds after the encounter starts, the run stops.

Settings:
- `Auto delete dungeon channel after a win` controls whether the workflow clicks the red `Delete dungeon channel` button after a successful run.
- The cached profile name is persisted in settings but is not user-editable.
- The pre-dungeon trade phase uses whatever channel is currently open on the `Bot tab`.
- The dungeon signup phase uses `Dungeon listing channel URL`, with fallback to the default `Channel URL`.

Automation IDs:
- Launcher: `CompleteDungeonButton`
- Browser tab: `DungeonBrowserTab`
- Browser surface: `DungeonDiscordWebView`
