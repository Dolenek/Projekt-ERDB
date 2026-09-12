# Duel Automation

`EpicRPGBot.UI` provides an exclusive duel workflow through `Duel start` on the dedicated `Duel tab`.

Fixed Discord scope:
- Guild: `792124157117988907`
- Duel category: `792253026873245708`
- Outgoing challenges: `dueling-2` (`792125583374024754`)
- Listings use fixed channel IDs: `road-to-50` (`1262467703902437456`), `road-to-200` (`1262468343135342703`), `road-to-500` (`1262468897974517840`), `road-to-1500` (`1262468944208592898`), `road-to-3000` (`1262469016803610775`), and `road-to-6000+` (`1262476104564740199`).
- Incoming challenges use fixed IDs: `dueling-1` (`792125562557562880`), `dueling-2` (`792125583374024754`), `dueling-3` (`1036018255812366377`), and `dueling-4` (`1062896821950742638`).
- Duel navigation rejects every URL outside the ten fixed listing and duel channels in guild `792124157117988907`.

Matchmaking:
1. Send `rpg p` in the channel currently open on the `Bot tab` and require both the profile name and a positive level.
2. Load and validate the fixed listing and duel channel catalog.
3. Scan each listing band that intersects the inclusive opponent range from half to twice the player level.
4. Collect `cf` listings no older than 15 minutes, excluding the player, missing author IDs, `nf`, standalone `gxp`, and messages already marked `✅` or `AFK`.
5. Sort all collected offers oldest first, regardless of source channel.
6. Re-read each candidate and add `✅` before challenging it. A failed reaction skips that candidate without pinging it.
7. Pause the normal engine and send one non-retryable `rpg duel <authorId>` in `dueling-2`.
8. Confirm an initiator-only `yes` prompt when present. Never accept the `yes` button addressed to the opponent.
9. After acceptance, choose `:credit_card:` when available, otherwise the first available weapon, then wait for the result.

Outgoing recovery:
- A cancelled or unanswered duel marks the original listing `AFK`, resumes matchmaking automation, and tries the next queued offer.
- A busy response leaves the `✅` reaction in place, does not add `AFK`, and tries the next offer.
- An uncertain outgoing registration or active-duel state stops the workflow and deliberately leaves the engine stopped.
- The initial offer queue is scanned only once.

Feed fallback:
- When the initial queue is exhausted, post exactly `<level> cf` in the player's own listing band.
- Keep the normal engine running while monitoring mention badges for all four duel channels without a timeout.
- Capture existing mention counts, remain in the player's listing channel, and navigate only when a fixed duel channel receives a new mention. Counts come from Discord's live unread state so monitoring continues while the Duel tab is hidden; rendered badges are a compatibility fallback.
- A handled badge must remain cleared for three polls before that channel is armed again. This prevents a stale or briefly missing sidebar badge from reopening the same channel.
- After a badge appears, wait up to 10 seconds in that channel for a new active request addressed to the profile name; unrelated mentions return to passive waiting.
- Accept the first new request addressed to the profile name, even if its level is outside the normal reward range.
- Pause the engine before finding and clicking `yes` by label with retries, then make no weapon choice, intentionally feeding the opponent.
- A cancellation resumes monitoring with the same listing. A win, loss, or tie ends the workflow.

Engine and stop behavior:
- Profile loading, listing scans, and passive incoming waits do not require the normal engine to stop.
- A real outgoing or accepted incoming challenge does require it to stop.
- Terminal completion restores the engine only when it was running at `Duel start`, and the normal resume path requests a fresh `rpg cd`.
- `Stop Duel` during safe waiting deletes the bot's own listing and restores the initial engine state.
- Cleanup first reloads the listing history; an offer already removed by the server is treated as successfully cleaned up, while a present offer uses retried menu and confirmation actions.
- `Stop Duel` after a challenge may be active leaves the engine stopped for manual completion.
- All workflow messages use the `[duel]` log prefix.
- The console logs every duel WebView navigation with channel name, ID, and URL, plus every command or listing sent by the workflow. The Bot-tab profile command is logged separately.

Automation IDs:
- Launcher: `DuelButton`
- Browser tab: `DuelBrowserTab`
- Browser surface: `DuelDiscordWebView`
